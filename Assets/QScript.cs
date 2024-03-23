using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using UnityEngine.Video;

public class QScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    public KMSelectable QSel;
    public GameObject PictureObj;
    public Texture[] PictureTextures;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private bool _isPopped;
    private bool _isSelected;
    private string _input;
    private int _chosenPicture;
    private string _solution;
    private static bool _multiLetter;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        var ModSelectable = GetComponent<KMSelectable>();
        ModSelectable.OnFocus += delegate () { _isSelected = true; };
        ModSelectable.OnDefocus += delegate () { _isSelected = false; };
        QSel.OnInteract += QPress;
        StartCoroutine(Rainbow());
        Debug.LogFormat("[Q #{0}] Q.", _moduleId);
    }

    private bool QPress()
    {
        if (_moduleSolved)
            return false;
        _isPopped = true;
        Audio.PlaySoundAtTransform("Pop", transform);
        QSel.AddInteractionPunch(1f);
        QSel.gameObject.transform.localScale = new Vector3(0f, 0f, 0f);
        newSolution:
        _chosenPicture = Rnd.Range(0, PictureTextures.Length);
        var name = PictureTextures[_chosenPicture].name;
        var ix = name.IndexOf(' ');
        if (ix == -1)
            _solution = name;
        else
            _solution = name.Substring(0, ix);
        if (_solution.Length > 1 && !_multiLetter)
            goto newSolution;
        PictureObj.GetComponent<MeshRenderer>().material.mainTexture = PictureTextures[_chosenPicture];
        PictureObj.SetActive(true);
        return false;
    }

    private IEnumerator Rainbow()
    {
        while (true)
        {
            var duration = Rnd.Range(6.9f, 13.37f);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                QSel.gameObject.GetComponent<MeshRenderer>().material.color = Color.HSVToRGB(Mathf.Lerp(0f, 1f, elapsed / duration), 0.69f, 1f);
                yield return null;
                elapsed += Time.deltaTime;
            }
            yield return null;
        }
    }

    private void OnGUI()
    {
        if (!_isSelected || !_isPopped || _moduleSolved)
            return;
        Event e = Event.current;
        if (e.type != EventType.KeyDown)
            return;
        ProcessKey(e.keyCode);
    }

    private void OnDestroy()
    {
        _multiLetter = true;
    }

    private void ProcessKey(KeyCode key)
    {
        QSel.AddInteractionPunch(0.2f);
        if (key < KeyCode.A || key > KeyCode.Z)
            return;
        string add = key.ToString().ToUpperInvariant();
        _input += add;
        if (_input.Contains(_solution))
        {
            Debug.LogFormat("[Q #{0}] {1}!!!", _moduleId, PictureTextures[_chosenPicture].name);
            Audio.PlaySoundAtTransform("beagle", transform);
            _moduleSolved = true;
            Module.HandlePass();
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press Q [Presses T Q] | !{0} type ABC [Types letters]";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (command.EqualsIgnoreCase("press q"))
        {
            if (_isPopped)
            {
                yield return "sendtochaterror T Q has already been pressed!";
                yield break;
            }
            yield return null;
            QSel.OnInteract();
            yield break;
        }
        if (command.ToLowerInvariant().StartsWith("type "))
        {
            string chars = command.Substring(5);
            for (int i = 0; i < chars.Length; i++)
            {
                if (!"ABCDEFGHIJKLMNOPQRSTUVWXYZ".Contains(chars.ToUpperInvariant()[i]))
                {
                    yield return "sendtochaterror!f '" + chars[i] + "' is not a letter!";
                    yield break;
                }
            }
            if (!_isPopped)
            {
                yield return "sendtochaterror T Q must B pressed first!";
                yield break;
            }
            yield return null;
            for (int i = 0; i < chars.Length; i++)
            {
                ProcessKey((KeyCode)("ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(chars.ToUpperInvariant()[i]) + 97));
                yield return new WaitForSeconds(.1f);
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        _isSelected = true;
        if (!_isPopped)
        {
            QSel.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        for (int i = 0; i < _solution.Length; i++)
        {
            ProcessKey((KeyCode)("ABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(_solution[i]) + 97));
            yield return new WaitForSeconds(.1f);
        }
        _isSelected = false;
    }
}
