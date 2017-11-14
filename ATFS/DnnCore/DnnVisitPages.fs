module DnnVisitPages

open System.Diagnostics
open canopy

// visits the given pages and makes sure no error is displayen in the result page
let visitPages (pages : string List) = 
    let pageErrSelector = pageNotFoundText
    let elmtErrSelector = "//*[contains(@class,'MessageHeading') and contains(.,'Error:')]"
    let stopWatch = Stopwatch()

    // returns failed pages in reverse order (last to first)
    let rec visitAll pages anyfail index total failedPages = 
        match pages with
        | [] -> failedPages
        | page :: pages -> 
            //printf "  page %i of %i: %A" index total page
            printf "  page %i of %i: " index total
            stopWatch.Restart()
            goto page
            let elapsed = stopWatch.ElapsedMilliseconds

            let failed = 
                existsAndVisible pageErrSelector || existsAndVisible elmtErrSelector || (try 
                                                                                             isOnPage page
                                                                                             false
                                                                                         with _ -> true) // fail if not on the page
            if failed then printf "  xxx FAILED xxx"
            else printf "  --- OK ---"
            printfn " (time = %i msec)" elapsed
            visitAll pages (failed || anyfail) (index + 1) total (if failed then (page :: failedPages) else failedPages)

    printfn "  Checking %i pages on the site" pages.Length
    let failedPages = visitAll pages false 1 pages.Length []
    if failedPages.Length > 0 then
        printfn "  The following pages failed inspection: %A" failedPages
    failedPages

// must be logged in as a user who have access to navigation bar first
let collectNavBarPages() = 
    let rec navBarLinks searchIds links = 
        match searchIds with
        | [] -> links
        | hd :: tl -> navBarLinks tl (links @ collectLinks hd)

    let navBarLinksIds = [ "#controlbar_admin_basic"; "#controlbar_admin_advanced"; "#controlbar_host_basic"; "#controlbar_host_advanced" ]

    let parent1 = 
        element "#controlbar_admin_basic"
        |> parent
        |> parent
        |> elementsWithin "a"
        |> List.head

    let parent2 = 
        element "#controlbar_host_basic"
        |> parent
        |> parent
        |> elementsWithin "a"
        |> List.head

    navBarLinks navBarLinksIds [ parent1.GetAttribute("href")
                                 parent2.GetAttribute("href") ]
