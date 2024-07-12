using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text artistText;
    [SerializeField] private TMP_Text playPauseButtonText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button startButton;
    [SerializeField] private Button playPauseButton;
    [SerializeField] private PlayBar progressBar;

    private bool hasInstance = false;
    private bool playing = false;
    private float timer = 0;

    private void Awake()
    {
        startButton.onClick.AddListener(() =>
        {
            if (inputField.text == "") return;
            if (!File.Exists(inputField.text))
            {
                Debug.LogError("File not found: " + inputField.text);
                return;
            }

            progressBar.SetValue(0);
            timer = 0;
            hasInstance = true;
            playing = true;
            PlayerManager.Instance.StartPlayerInstance(inputField.text);
        });

        playPauseButton.onClick.AddListener(() =>
        {
            if (playing)
            {
                playing = false;
                PlayerManager.Instance.Pause();
                playPauseButtonText.text = "Play";
            }
            else
            {
                playing = true;
                PlayerManager.Instance.Play();
                playPauseButtonText.text = "Pause";
            }
        });

        progressBar.OnValueChanged += (value) =>
        {
            float timeStamp = value* PlayerManager.Instance.GetDuration();
            
            timer = timeStamp;
            PlayerManager.Instance.SkipTo(timeStamp);
        };

        PlayerManager.OnMetadataChanged += PlayerManager_OnMetadataChanged;
    }

    private void Update()
    {
        if (hasInstance && playing)
        {
            timer += Time.deltaTime;
            progressBar.SetValue(timer / PlayerManager.Instance.GetDuration());
        }
    }

    private void PlayerManager_OnMetadataChanged(PlayerManager.SongMetadata metadata)
    {
        titleText.text = metadata.title;
        artistText.text = metadata.artist;

        Debug.Log("Metadata other: " + metadata.year + " - " + metadata.album);
    }
}
