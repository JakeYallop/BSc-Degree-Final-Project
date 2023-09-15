using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Web;
public sealed class JsonValueConverter<T> : ValueConverter<T, string>
{
    public static readonly JsonValueConverter<T> Instance = new();
    public JsonValueConverter() : base(x => JsonSerializer.Serialize(x, JsonOptions.Default), x => JsonSerializer.Deserialize<T>(x, JsonOptions.Default)!)
    {
    }
}
