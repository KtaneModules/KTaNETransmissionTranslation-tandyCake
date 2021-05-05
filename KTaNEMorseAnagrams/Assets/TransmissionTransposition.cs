using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class TransmissionTransposition : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] buttons;
    public KMSelectable delete;
    public GameObject[] displays;
    public TextMesh[] displayTexts;
    public Material[] colors;
    public GameObject bg;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    bool isAnimating;
    char[] keyboardLayout = "QWERTYUIOPASDFGHJKLZXCVBNM".ToCharArray();
    int[] dotCounts = new int[] { 1, 3, 2, 2, 1, 3, 1, 4, 2, 1, 1, 3, 0, 1, 0, 2, 1, 2, 3, 0, 2, 3, 1, 2, 1, 2 };
    int[] dashCounts = new int[] { 1, 1, 2, 1, 0, 1, 2, 0, 0, 3, 2, 1, 2, 1, 3, 2, 3, 1, 0, 1, 1, 1, 2, 2, 3, 2 };
    string display = string.Empty;
    string inputBox = string.Empty;
    string toDisplay;

    int displayDots;
    int displayDashes;

    List<string> possibleAnswers = new List<string>();


    void Awake()
    {
        moduleId = moduleIdCounter++;

        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { KeyPress(button, button.GetComponentInChildren<TextMesh>().text); return false; };
        delete.OnInteract += delegate () { Clear(); return false; };

    }

    void Start()
    {
        GetDisplay();
        CalculateAnswers();
        if (possibleAnswers.Count < 60)
            Start();
        else
        {
            Debug.LogFormat("[Transmission Transposition #{0}] The displayed word is {1}, which has a total of {2} dots and {3} dashes.", moduleId, display, displayDots, displayDashes);
            Debug.LogFormat("[Transmission Transposition #{0}] Possible answers are: {1}", moduleId, possibleAnswers.Join());
        }

    }
    int GetDotCount(string input)
    {
        int dots = 0;
        foreach (char letter in input.ToUpperInvariant())
            dots += dotCounts[letter - 'A'];
        return dots;
    }
    int GetDashCount(string input)
    {
        int dashes = 0;
        foreach (char letter in input.ToUpperInvariant())
            dashes += dashCounts[letter - 'A'];
        return dashes;
    }
    bool ValidCheck(string first, string second)
    {
        int cnt = 0;
        for (int i = 0; i < 5; i++)
            if (first.Contains(second[i])) cnt++;
        return cnt <= 2;
    }

    void GetDisplay()
    {
        display = wordList.Phrases[UnityEngine.Random.Range(0, wordList.Phrases.Length)];
        displayTexts[0].text = display;
        displayDots = GetDotCount(display);
        displayDashes = GetDashCount(display);
    }
    void CalculateAnswers()
    {
        foreach (string word in wordList.Phrases)
        {
            if ((GetDotCount(word) == displayDots) && (GetDashCount(word) == displayDashes) && ValidCheck(display, word))
                    possibleAnswers.Add(word);
        }
    }

    void KeyPress(KMSelectable button, string letter)
    {
        button.AddInteractionPunch(0.05f);
        Audio.PlaySoundAtTransform("beep", transform);
        if (moduleSolved || isAnimating)
            return;
        inputBox += letter;

        displayTexts[1].text = inputBox.PadLeft(5, '-');

        if (inputBox.Length == 5)
        {
            button.AddInteractionPunch(1f);
            Submit();
        }
    }

    void Submit()
    {
        if (possibleAnswers.Contains(inputBox))
        {
            GetComponent<KMBombModule>().HandlePass();
            moduleSolved = true;
            Audio.PlaySoundAtTransform("solve", transform);
            Debug.LogFormat("[Transmission Transposition #{0}] You submitted {1}. Module solved.", moduleId, inputBox);
            if (UnityEngine.Random.Range(0, 100) == 0)
            {
                displays[0].GetComponent<MeshRenderer>().material = colors[4];
                displays[1].GetComponent<MeshRenderer>().material = colors[4];
                bg.GetComponent<MeshRenderer>().material.color = new Color(0.663f, 1, 1);
            }
            else
            {
                displays[0].GetComponent<MeshRenderer>().material = colors[1];
                displays[1].GetComponent<MeshRenderer>().material = colors[1];
            }
        }
        else if ((displayDots == GetDotCount(inputBox)) && (displayDashes == GetDashCount(inputBox)))
            StartCoroutine(Invalid());
        else StartCoroutine(Strike());
    }
    void Clear()
    {
        delete.AddInteractionPunch(0.3f);
        Audio.PlaySoundAtTransform("beep", transform);
        if (moduleSolved || isAnimating)
            return;
        inputBox = string.Empty;
        displayTexts[1].text = "-----";
    }

    IEnumerator Invalid()
    {
        if (!ValidCheck(display, inputBox))
            Debug.LogFormat("[Transmission Transposition #{0}] You submitted {1}, which has more than 2 letters in common.", moduleId, inputBox);
        else Debug.LogFormat("[Transmission Transposition #{0}] You submitted {1}, which is not in the word list.", moduleId, inputBox);
        Audio.PlaySoundAtTransform("error", transform);

        isAnimating = true;
        displays[0].GetComponent<MeshRenderer>().material = colors[2];
        displays[1].GetComponent<MeshRenderer>().material = colors[2];
        yield return new WaitForSecondsRealtime(1f);
        inputBox = string.Empty;
        displayTexts[1].text = "-----";
        displays[0].GetComponent<MeshRenderer>().material = colors[0];
        displays[1].GetComponent<MeshRenderer>().material = colors[0];
        isAnimating = false;
    }
    IEnumerator Strike()
    {
        Debug.LogFormat("[Transmission Transposition #{0}] You submitted {1}, which does not have the same number of dots/dashes, Strike incurred.", moduleId, inputBox);
        Audio.PlaySoundAtTransform("buzzer", transform);
        isAnimating = true;
        displays[0].GetComponent<MeshRenderer>().material = colors[3];
        displays[1].GetComponent<MeshRenderer>().material = colors[3];
        yield return new WaitForSecondsRealtime(1f);
        inputBox = string.Empty;
        displayTexts[1].text = "-----";
        displays[0].GetComponent<MeshRenderer>().material = colors[0];
        displays[1].GetComponent<MeshRenderer>().material = colors[0];
        isAnimating = false;
        GetComponent<KMBombModule>().HandleStrike();
    }


#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use [!{0} submit MORSE] to enter the word MORSE into the module.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string input)
    {
        string command = input.Trim().ToUpperInvariant();
        List<string> parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (Regex.IsMatch(command, @"^SUBMIT\s+[A-Z]{5}$"))
        {
            yield return null;
            if (inputBox != string.Empty)
            {
                delete.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            foreach (char letter in parameters.Last())
            {
                buttons[Array.IndexOf(keyboardLayout, letter)].OnInteract();
                yield return new WaitForSeconds(0.1f);  
            }
        }


    }

    IEnumerator TwitchHandleForcedSolve()
    {
        string weAreSubmittingThisNumberColonSmileColon = possibleAnswers.PickRandom();
        if (inputBox.Length != 0)
        {
            delete.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        foreach (char letter in weAreSubmittingThisNumberColonSmileColon)
        {
            buttons[Array.IndexOf(keyboardLayout, letter)].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}