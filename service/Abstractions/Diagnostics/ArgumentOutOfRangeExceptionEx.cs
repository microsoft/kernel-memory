// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;

// ReSharper disable CheckNamespace
namespace Microsoft.KernelMemory;

public static class ArgumentOutOfRangeExceptionEx
{
    // ======== Generics ========

    public static void ThrowIfEqual<T>(T value, T other, string? paramName, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(value, other)) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfNotEqual<T>(T value, T other, string? paramName, string message)
    {
        if (EqualityComparer<T>.Default.Equals(value, other)) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIf(bool condition, string? paramName, string message)
    {
        if (!condition) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfNot(bool condition, string? paramName, string message)
    {
        if (condition) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    // ======== int ========

    public static void ThrowIfZero(int value, string paramName, string message)
    {
        if (value != 0) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfNegative(int value, string paramName, string message)
    {
        if (value >= 0) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfZeroOrNegative(int value, string paramName, string message)
    {
        if (value > 0) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfLessThan(int value, int other, string paramName, string message)
    {
        if (value >= other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfEqualToOrLessThan(int value, int other, string paramName, string message)
    {
        if (value > other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfGreaterThan(int value, int other, string paramName, string message)
    {
        if (value <= other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfEqualToOrGreaterThan(int value, int other, string paramName, string message)
    {
        if (value < other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    // ======== uint ========

    public static void ThrowIfZero(uint value, string paramName, string message)
    {
        if (value != 0) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfNegative(uint value, string paramName, string message)
    {
        if (value >= 0) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfZeroOrNegative(uint value, string paramName, string message)
    {
        if (value > 0) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfLessThan(uint value, uint other, string paramName, string message)
    {
        if (value >= other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfEqualToOrLessThan(uint value, uint other, string paramName, string message)
    {
        if (value > other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfGreaterThan(uint value, uint other, string paramName, string message)
    {
        if (value <= other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfEqualToOrGreaterThan(uint value, uint other, string paramName, string message)
    {
        if (value < other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    // ======== float ========

    public static void ThrowIfZero(float value, string paramName, string message)
    {
        if (value != 0) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfNegative(float value, string paramName, string message)
    {
        if (value >= 0) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfZeroOrNegative(float value, string paramName, string message)
    {
        if (value > 0) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfLessThan(float value, float other, string paramName, string message)
    {
        if (value >= other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfEqualToOrLessThan(float value, float other, string paramName, string message)
    {
        if (value > other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfGreaterThan(float value, float other, string paramName, string message)
    {
        if (value <= other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfEqualToOrGreaterThan(float value, float other, string paramName, string message)
    {
        if (value < other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    // ======== double ========

    public static void ThrowIfZero(double value, string paramName, string message)
    {
        if (value != 0) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfNegative(double value, string paramName, string message)
    {
        if (value >= 0) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfZeroOrNegative(double value, string paramName, string message)
    {
        if (value > 0) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfLessThan(double value, double other, string paramName, string message)
    {
        if (value >= other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfEqualToOrLessThan(double value, double other, string paramName, string message)
    {
        if (value > other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfGreaterThan(double value, double other, string paramName, string message)
    {
        if (value <= other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }

    public static void ThrowIfEqualToOrGreaterThan(double value, double other, string paramName, string message)
    {
        if (value < other) { return; }

        throw new ArgumentOutOfRangeException(paramName, message);
    }
}
