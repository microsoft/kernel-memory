// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;

namespace Microsoft.KernelMemory.InteractiveSetup.UI;

internal static class SetupUI
{
    public static string AskPassword(string question, string? defaultValue, bool trim = true, bool optional = false)
    {
        return AskOpenQuestion(question: question, defaultValue: defaultValue, trim: trim, optional: optional, isPassword: true);
    }

    public static bool AskBoolean(string question, bool defaultValue)
    {
        string[] yes = { "YES", "Y" };
        string[] no = { "NO", "N" };
        while (true)
        {
            var answer = AskOpenQuestion(question: question, defaultValue: defaultValue ? "Yes" : "No", optional: false).ToUpperInvariant();
            if (yes.Contains(answer)) { return true; }

            if (no.Contains(answer)) { return false; }
        }
    }

    public static string AskOptionalOpenQuestion(string question, string? defaultValue)
    {
        return AskOpenQuestion(question: question, defaultValue: defaultValue, optional: true);
    }

    public static string AskOpenQuestion(string question, string? defaultValue, bool trim = true, bool optional = false, bool isPassword = false)
    {
        if (!string.IsNullOrEmpty(defaultValue))
        {
            question = isPassword ? $"{question} [current: ****hidden****]" : $"{question} [current: {defaultValue}]";
        }

        question = isPassword ? $"{question} (value will not show on screen): " : $"{question}: ";

        string answer = string.Empty;
        var done = false;
        while (!done)
        {
            string? newAnswer;
            if (isPassword)
            {
                newAnswer = ReadLine.ReadPassword(question);
                if (string.IsNullOrEmpty(newAnswer))
                {
                    newAnswer = defaultValue;
                }
            }
            else
            {
                newAnswer = ReadLine.Read(question, defaultValue);
            }

            answer = trim ? $"{newAnswer}".Trim() : $"{newAnswer}";

            done = (optional || !string.IsNullOrEmpty(answer));
        }

        return answer;
    }

    public static void AskQuestionWithOptions(QuestionWithOptions question)
    {
        void ShowQuestion(int selected)
        {
            Console.Clear();
            Console.WriteLine($"{question.Title}\n");
            ShowQuestionDescription(question.Description);

            for (int index = 0; index < question.Options.Count; index++)
            {
                Answer answer = question.Options[index];
                if (index == selected)
                {
                    Console.Write("> * ");
                }
                else
                {
                    Console.Write("  * ");
                }

                Console.WriteLine((string?)answer.Name);
            }
        }

        // Find the active option
        int current = 0;
        for (int index = 0; index < question.Options.Count; index++)
        {
            if (question.Options[index].IsSelected)
            {
                current = index;
                break;
            }
        }

        ShowQuestion(current);

        var maxPos = question.Options.Count - 1;
        var done = false;
        Action? action = null;
        while (!done)
        {
            // Always redraw, to take care of screen artifacts caused by keys pressed
            ShowQuestion(current);

            ConsoleKeyInfo pressedKey = Console.ReadKey();
            switch (pressedKey.Key)
            {
                // Move down
                case ConsoleKey.DownArrow:
                case ConsoleKey.PageDown:
                case ConsoleKey.Tab:
                case ConsoleKey.Spacebar:
                    if (current < maxPos) { current++; }

                    break;

                // Move up
                case ConsoleKey.UpArrow:
                case ConsoleKey.PageUp:
                case ConsoleKey.Backspace:
                    if (current > 0) { current--; }

                    break;

                // Reset
                case ConsoleKey.Home:
                case ConsoleKey.Clear:
                    current = 0;
                    break;

                // Go to end
                case ConsoleKey.End:
                    current = maxPos;
                    break;

                // Select current
                case ConsoleKey.Enter:
                    action = question.Options[current].OnSelected;
                    done = true;
                    break;

                // Exit
                case ConsoleKey.Escape:
                    action = Exit;
                    done = true;
                    break;
            }
        }

        Console.WriteLine();
        action?.Invoke();
    }

    public static void Exit()
    {
        Environment.Exit(0);
    }

    private static void ShowQuestionDescription(string desc)
    {
        if (string.IsNullOrEmpty(desc)) { return; }

        const int MaxLineLen = 72;
        var parts = desc.Split(' ');
        var count = 0;
        foreach (var p in parts)
        {
            if (count + 1 + p.Length <= MaxLineLen)
            {
                Console.Write(' ');
                count++;
            }
            else
            {
                Console.Write("\n ");
                count = 1;
            }

            Console.Write(p);
            count += p.Length;
        }

        Console.WriteLine("\n");
    }
}
