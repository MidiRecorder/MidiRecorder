[![Build status](https://github.com/icalvo/Icm.MidiRecorder/actions/workflows/ci.yml/badge.svg)](https://github.com/icalvo/Icm.MidiRecorder/actions/workflows/ci.yml)

[![Join the Gitter chat!](https://badges.gitter.im/gsscoder/commandline.svg)](https://gitter.im/gsscoder/commandline?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)


# MIDI Recorder

## Introduction

Every musician has improvised something that then was unable to remember precisely. With the current technology you can solve that problem: just fire your favorite DAW and click record on a MIDI track. But this system is a bit cumbersome: first of all you have to remember to open your DAW, or have it permanently opened. You need a separate project because probably you are working on other things. So maybe two DAW instances? And you need to open this secondary instance every time you restart your system! Maybe your DAW offers some feature for background MIDI recording, but, does it preserve the data if you close the DAW (or it is closed accidentally)? How easy is to navigate the recorded MIDI data after, say, 3 days, in which you have maybe played at several points of the day?

If you think that there must be an alternative way, you are right!

**MIDI Recorder** is a little tool that allows you to record into MIDI files everything you play on your MIDI controllers. You can easily set MIDI Recorder to run when your system starts. MIDI Recorder splits what you play based on silences and date/time. You can set up a custom folder and filename structure based on date/time, some characteristics of the the MIDI Input, and other available properties. You can have it running while you work with your favorite DAW.

## Usage

The typical usage would be:
```
    midirecord.exe MIDI_INPUT_NAME DELAY_FOR_FILE_SPLIT OUTPUT_MIDI_PATH_FORMAT
```

For example, let's say you want to track the MIDI activity of your Korg M1, and you want to save a file each time you stop playing for 10 seconds. Also, you want to store the files in a path like `{YEAR}/{MONTH}/{DAY}/{HHMMSS}_{NUMBEROFNOTES}.mid`. For example if you stopped playing at 2021-07-14 14:32, and you've played 421 notes, it will save (10 seconds later) the file: `2021/07/14/1432_421.mid`. And then it will wait for your next improv! Well, in that case you would start MIDI Recorder like this:

```
    midirecord.exe "Korg M1" 10000 "{Now:yyyy}/{Now:MM}/{Now:dd}/{Now:HHmmss}_{NumberOfNotes}.mid"
```

The most complex part here is the path format. Please refer to the **Path Formatting** section for complete help.

If you don't know the name of your MIDI inputs, you can use the `list` verb to get a list of all THE MIDI inputs in your system:

```
midirecord.exe list
```

## Path Formatting

