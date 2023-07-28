// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

namespace Microsoft.SemanticMemory.InteractiveSetup;

public sealed class QuestionWithOptions
{
    public string Title { get; set; } = string.Empty;
    public List<Answer> Options { get; set; } = new();
}

public sealed class Answer
{
    public string Name { get; }
    public Action Selected { get; }

    public Answer(string name, Action selected)
    {
        this.Name = name;
        this.Selected = selected;
    }
}

public static class SetupUI
{
    public static string AskPassword(string question, string? defaultValue, bool trim = true, bool optional = false)
    {
        return AskOpenQuestion(question: question, defaultValue: defaultValue, trim: trim, optional: optional, isPassword: true);
    }

    public static string AskOpenQuestion(string question, string? defaultValue, bool trim = true, bool optional = false, bool isPassword = false)
    {
        if (!string.IsNullOrEmpty(defaultValue))
        {
            question = isPassword ? $"{question} [current: ****hidden****]" : $"{question} [current: {defaultValue}]";
        }

        question = isPassword ? $"{question} (value will not appear): " : $"{question}: ";

        string answer = string.Empty;
        var done = false;
        while (!done)
        {
            // Console.Clear();
            // Console.WriteLine(question);
            // var newAnswer = Console.ReadLine();
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

        int current = 0;
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
                    action = question.Options[current].Selected;
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
}
