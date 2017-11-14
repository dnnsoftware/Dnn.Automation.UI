module Scheduler

open DnnCanopyContext
open DnnScheduler
open DnnUserLogin

let private openSchedulersList()=
    logOff()
    loginAsHost()
    openPBScheduler()
    //click Scheduler tab
    waitClick "div.taskScheduler-app>div>div>div>div>ul>li:nth-of-type(2)"
    waitForAjax()

let bvtTests _ =
    context "Host Schedulers: UI Tests"

    "Schedulers | Host | Check all Host Schedulers exist" @@@ fun _ ->
        openSchedulersList()
        let mutable failedReasons = ""
        failedReasons <- platformHostSchedulers |> List.fold (fun acc item -> acc + (checkSchedulerExists item.Name)) failedReasons
        if failedReasons <> "" then failwithf "  FAIL: %s" failedReasons

let all _ = 
    bvtTests()
