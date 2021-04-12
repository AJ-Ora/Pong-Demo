
using UnityEngine;
using UnityEngine.UI;

public class Scoreboard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Text playerOneScoreText = null;
    [SerializeField] private Text playerTwoScoreText = null;

    private byte _playerOneScore = 0;
    private byte _playerTwoScore = 0;

    public byte PlayerOneScore
    {
        get => _playerOneScore;
        set
        {
            if (playerOneScoreText != null) playerOneScoreText.text = value.ToString();
            _playerOneScore = value;
        }
    }
    public byte PlayerTwoScore
    {
        get => _playerTwoScore;
        set
        {
            if (playerTwoScoreText != null) playerTwoScoreText.text = value.ToString();
            _playerTwoScore = value;
        }
    }

    private bool _graphicsEnabled = true;
    public bool GraphicsEnabled
    {
        get => _graphicsEnabled;
        set
        {
            if (value == _graphicsEnabled) return;
            playerOneScoreText.enabled = value;
            playerTwoScoreText.enabled = value;
            _graphicsEnabled = value;
        }
    }
}
