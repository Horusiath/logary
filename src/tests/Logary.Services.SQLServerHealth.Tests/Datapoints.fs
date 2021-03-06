﻿module Datapoints

open Fuchu

type Assert =
  static member Contains(msg : string, xExpected : 'a, xs : 'a seq) =
    match Seq.tryFind ((=) xExpected) xs with
    | None -> Tests.failtestf "%s -- expected %A to contain %A" msg xs xExpected
    | Some _ -> ()

open Logary
open Logary.Metrics

[<Tests>]
let datapoints =
  let dps = SQLServerIOInfo.Impl.dps "drive" "c" |> Set.ofList |> Set.map DP.joined
  testList "getting all datapoints" [
    testCase "for drive c" <| fun _ ->
      Assert.Equal(
        "should eq set",
        [ "logary.sql_server_health.drive_io_stall_read.c"
          "logary.sql_server_health.drive_io_stall_write.c"
          "logary.sql_server_health.drive_io_stall.c"
          "logary.sql_server_health.drive_num_of_reads.c"
          "logary.sql_server_health.drive_num_of_writes.c"
          "logary.sql_server_health.drive_num_of_bytes_read.c"
          "logary.sql_server_health.drive_num_of_bytes_written.c"
          "logary.sql_server_health.drive_read_latency.c"
          "logary.sql_server_health.drive_write_latency.c"
          "logary.sql_server_health.drive_latency.c"
          "logary.sql_server_health.drive_bytes_per_read.c"
          "logary.sql_server_health.drive_bytes_per_write.c"
          "logary.sql_server_health.drive_bytes_per_transfer.c"
          ] |> Set.ofList,
        dps)
    ]