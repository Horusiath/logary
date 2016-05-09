﻿module Logary.Tests.Formatting

open System

open Fuchu
open NodaTime

open Logary
open Logary.Formatting

open Logary.Tests.TestDSL

let private sampleMessage : Message =
  { name      = PointName.ofList ["a"; "b"; "c"; "d"]
    value     = Event "this is bad"
    fields    = Map.empty
    context   = Map.empty
    timestamp = Instant.FromSecondsSinceUnixEpoch(3L).PlusTicks(1234567L).Ticks * 100L
    level     = LogLevel.Error }

[<Tests>]
let tests =
  testList "formatting" [
    testCase "StringFormatter.Verbatim" <| fun _ ->
      (because "formatting the message verbatim" <| fun _ ->
        Message.eventError "hello world"
        |> StringFormatter.verbatim.format)
      |> should equal "hello world"
      |> thatsIt

    testCase "StringFormatter.VerbatimNewline" <| fun _ ->
      (because "logging verbatim with newline" <| fun () ->
        Message.eventInfo "hi there"
        |> StringFormatter.verbatimNewLine.format)
      |> should equal (sprintf "hi there%s" Environment.NewLine)
      |> thatsIt

    testCase "StringFormatter.VerbatimNewlineTemplated" <| fun _ ->
      (because "logging verbatim with newline, templated" <| fun () ->
        {(Message.eventInfo "what's {@direction}") with fields = [(PointName.ofSingle "direction", Field (String "up", None))] |> Map.ofList}
        |> StringFormatter.verbatimNewLine.format)
      |> should equal (sprintf "what's up%s" Environment.NewLine)
      |> thatsIt

    testCase "StringFormatter.LevelDatetimePathMessageNl no exception" <| fun _ ->
      (because "logging with LevelDatetimePathMessageNl" <| fun () ->
        sampleMessage |> StringFormatter.levelDatetimeMessagePathNl.format)
      |> should equal (
          sprintf "E 1970-01-01T00:00:03.1234567+00:00: this is bad [a.b.c.d]%s"
            Environment.NewLine)
      |> thatsIt

    testCase "StringFormatter.LevelDatetimePathMessageNl no exception, data" <| fun _ ->
      (because "logging with LevelDatetimePathMessageNl" <| fun () ->
        { sampleMessage with
            fields = Map [PointName.ofSingle "a", Field (String "b", None); PointName.ofSingle "a2", Field (Int64 24L, None) ]
            context = Map ["a", String "b"]}
        |> StringFormatter.levelDatetimeMessagePathNl.format)
      |> should equal (
          sprintf "E 1970-01-01T00:00:03.1234567+00:00: this is bad [a.b.c.d]%s  a => \"b\"%s  a2 => 24%s  Context:%s    a => \"b\"%s"
            Environment.NewLine Environment.NewLine Environment.NewLine Environment.NewLine Environment.NewLine)
      |> thatsIt

    testCase "StringFormatter.LevelDatetimePathMessageNl no exception, data, list with map in it" <| fun _ ->
      (because "logging with LevelDatetimePathMessageNl" <| fun () ->
        { sampleMessage with
            fields = [ PointName.ofSingle "a",  (Field (String "b", None))
                       PointName.ofSingle "a2", (Field (Int64 24L, None))
                       PointName.ofSingle "things",
                         (Field (Array
                           [ Int64 1L
                             Int64 2L
                             Object <| Map ["1", String "hello"] ], None))
                     ] |> Map.ofList
        }
        |> StringFormatter.levelDatetimeMessagePathNl.format)
      |> should equal (
          String.Format("E 1970-01-01T00:00:03.1234567+00:00: this is bad [a.b.c.d]" +
                        "{0}  a => \"b\"{0}  a2 => 24{0}  things => {0}    - 1{0}    - 2{0}    - {0}      1 => \"hello\"{0}",
                        Environment.NewLine))
      |> thatsIt

    testCase "StringFormatter.LevelDatetimePathMessageNl no exception, nested data" <| fun _ ->
      (because "logging with LevelDatetimePathMessageNl" <| fun () ->
        { sampleMessage with
            fields =
              [ PointName.ofSingle "a", (Field (["b", Int64 1L] |> Map.ofList |> Object, None))
                PointName.ofSingle "c", (Field (Int64 2L, None))
              ] |> Map.ofList
        }
        |> StringFormatter.levelDatetimeMessagePathNl.format)
      |> should equal (
          String.Format("E 1970-01-01T00:00:03.1234567+00:00: this is bad [a.b.c.d]" +
                        "{0}  a => {0}    b => 1{0}  c => 2{0}", Environment.NewLine))
      |> thatsIt

    testCase "StringFormatter.LevelDatetimePathMessageNl with exception" <| fun _ ->
      let e = new Exception("Gremlings in the machinery")
      (because "logging with exception attached" <| fun () ->
        sampleMessage
        |> Message.addExn e
        |> StringFormatter.levelDatetimeMessagePathNl.format)
      |> should equal (
        String.Format("E 1970-01-01T00:00:03.1234567+00:00: this is bad [a.b.c.d]" +
                      "{0}  errors => {0}    - {0}      hResult => -2146233088{0}      message => \"{1}\"{0}      type => \"{2}\"{0}",
                      Environment.NewLine, e.Message, (e.GetType ()).FullName))
      |> thatsIt

    testCase "StringFormatter.LevelDatetimePathMessageNl with exception, data" <| fun _ ->
      let e = new Exception("Gremlings in the machinery")
      (because "logging with exception attached" <| fun () ->
        { sampleMessage with fields = [PointName.ofSingle "a", Field (String "b", None); PointName.ofSingle "a2", Field (Int64 24L, None) ] |> Map.ofList }
        |> Message.addExn e
        |> StringFormatter.levelDatetimeMessagePathNl.format)
      |> should equal (
        String.Format("E 1970-01-01T00:00:03.1234567+00:00: this is bad [a.b.c.d]"+
                      "{0}  a => \"b\"{0}  a2 => 24{0}" +
                      "  errors => {0}    - {0}      hResult => -2146233088{0}      message => \"{1}\"{0}      type => \"{2}\"{0}",
                      Environment.NewLine, e.Message, (e.GetType ()).FullName))
      |> thatsIt

    testCase "``JsonFormatter has no newline characters``" <| fun _ ->
      (because "logging message with newline in it" <| fun () ->
          { sampleMessage with value = Event "here\n  we\ngo!" } |> JsonFormatter.Default.format)
      |> should equal ("""{"context":{},"fields":{},"level":"error","name":["a","b","c","d"],""" +
                       """"timestamp":3123456700,"value":{"event":"here\n  we\ngo!"}}""")
      |> thatsIt

    testCase "Formatting.templateFromFormat, simple case" <| fun _ ->
      let format = "This {0} contains {1} words."
      let args : obj[] = [|"sentence"; 4|]
      (because "converting a String.Format into a message template" <| fun () ->
        Field.templateFromFormat format args)
      |> should equal ("This {arg0} contains {arg1} words.",
                       [ "arg0", Field (String "sentence", None)
                         "arg1", Field (Int64 4L, None) ])
      |> thatsIt

    ]
