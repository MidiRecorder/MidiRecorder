using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MidiRecorder
{
    public static class StringEx
    {
        public static string Format(string formatString, object data)
        {
            object? GetPropertyValue(object target, string propertyName) 
            {
                var propertyInfo = target.GetType().GetProperty(propertyName);
                if (propertyInfo == null)
                {
                    if (target is ValueTuple)
                    {
                        throw new ArgumentException(
                            $"Property {propertyName} not found. Don't ValueTuple like (PropA:ValA, PropB: ValB). Instead use anonymous object syntax: new {{ PropA = ValA, PropB = ValB }}.",
                            nameof(propertyName));
                    }

                    throw new ArgumentException(
                        $"Property {propertyName} not found.",
                        nameof(propertyName));
                }

                return propertyInfo.GetValue(target, null);
            }

            var (format, itemNames) = TranslateToStandardFormatString(formatString);
            return string.Format(
                CultureInfo.InvariantCulture,
                format,
                itemNames.Select(itemName => GetPropertyValue(data, itemName)).ToArray());
        }


        public static (string format, List<string> itemNames) TranslateToStandardFormatString(string formatString)
        {
            if (string.IsNullOrWhiteSpace(formatString))
            {
                throw new ArgumentException($"'{nameof(formatString)}' cannot be null or whitespace.", nameof(formatString));
            }

            var stringFormatBuilder = new StringBuilder(formatString.Length);
            var state = ParseState.Outside;
            var tagNameBuilder = new StringBuilder();
            var itemNames = new List<string>();

            int GetIndexFor(string itemName)
            {
                var existingItem = itemNames.IndexOf(itemName);
                if (existingItem == -1)
                {
                    itemNames.Add(itemName);

                    return itemNames.Count - 1;
                }

                return existingItem;
            }

            foreach (var character in formatString)
            {
                switch ((state, character))
                {
                    case (ParseState.Outside, '{'):
                        stringFormatBuilder.Append(character);
                        state = ParseState.OpenBracket;
                        break;
                    case (ParseState.Outside, _):
                        stringFormatBuilder.Append(character);
                        break;
                    case (ParseState.OpenBracket, '{'):
                        stringFormatBuilder.Append(character);
                        state = ParseState.Outside;
                        break;
                    case (ParseState.OpenBracket, '}'):
                    case (ParseState.OpenBracket, ','):
                    case (ParseState.OpenBracket, ':'):
                        throw new FormatException("Cannot have an empty item name");
                    case (ParseState.OpenBracket, _):
                        tagNameBuilder.Append(character);
                        state = ParseState.ItemName;
                        break;
                    case (ParseState.ItemName, ','):
                    case (ParseState.ItemName, ':'):
                        stringFormatBuilder.Append(GetIndexFor(tagNameBuilder.ToString()));
                        stringFormatBuilder.Append(character);
                        tagNameBuilder.Clear();
                        state = ParseState.ItemAlignmentOrFormat;
                        break;
                    case (ParseState.ItemName, '}'):
                        stringFormatBuilder.Append(GetIndexFor(tagNameBuilder.ToString()));
                        stringFormatBuilder.Append(character);
                        tagNameBuilder.Clear();
                        state = ParseState.Outside;
                        break;
                    case (ParseState.ItemName, _):
                        tagNameBuilder.Append(character);
                        break;
                    case (ParseState.ItemAlignmentOrFormat, '}'):
                        stringFormatBuilder.Append(character);
                        state = ParseState.Outside;
                        break;
                    case (ParseState.ItemAlignmentOrFormat, _):
                        stringFormatBuilder.Append(character);
                        break;
                }
            }
            return (stringFormatBuilder.ToString(), itemNames);       
        }

        private enum ParseState
        {
            Outside,
            OpenBracket,
            ItemName,
            ItemAlignmentOrFormat,
        }
    }
}