module DnnScheduler

open canopy

/// <summary>Opens the Host Scheduler Section</summary>
/// <param name="schedulerName"> The name of the Host Scheduler</param>
let openHostScheduler schedulerName =    
    openPBScheduler()
    //click Scheduler tab
    click "div.taskScheduler-app>div>div>div>div.primary>ul>li:nth-child(2)"
    waitForAjax()
    //check scheduler exists
    let schedulerRows = "//div[contains(@class,'schedule-items-grid')]/div"
    let schedulerDiv = schedulerRows + sprintf "/div/div/div[@title=\"%s\"]" schedulerName
    if not(exists schedulerDiv) then
        failwithf "  FAIL: Host Scheduler %A not found" schedulerName
    scrollTo schedulerDiv
    //open scheduler
    let editIcon = schedulerDiv + "/../div/div[@class='edit-icon']/*"
    click editIcon
    waitLoadingBar() 

/// <summary> Runs a Host Scheduler. Does not wait for the Scheduler to finish. 
/// If the Host Scheduler is not runnable, returns false. </summary>
/// <param name="schName"> The name of the Host Scheduler to run. </param>
/// <returns> True if Host Scheduler was found and Run button was visible. Otherwise, returns false.  </returns>
let runHostScheduler schName =
    openHostScheduler schName
    let schedulerRows = "//div[contains(@class,'schedule-items-grid')]/div"
    let schedulerDiv = schedulerRows + sprintf "/div/div/div[@title=\"%s\"]" schName
    let runNowBtn = schedulerDiv + "/../../../div/div/div[@class='scheduler-setting-editor']/div/button[@role='secondary'][3]"
    //check if run now button visible and if so, click on it
    if not(exists runNowBtn) then false
    elif not(existsAndVisible runNowBtn) then false
    else
        scrollTo runNowBtn
        click runNowBtn
        waitLoadingBar()
        printfn "  INFO: Submitted Scheduler Job %A at %s" schName (System.DateTime.Now.ToString())
        sleep 3 //wait for job to start
        true

/// <summary> Runs a Host Scheduler. Waits for the Scheduler to finish. 
/// If the Host Scheduler is not runnable, returns false. </summary>
/// <param name="schName"> The name of the Host Scheduler to run. </param>
/// <param name="waitTime"> Time (in seconds) to wait for the job to complete. </param>
/// <returns> True if Host Scheduler was found, Run button was visible, and Scheduler Run completed successfully. Otherwise, returns false.  </returns>
let runHostSchedulerWaitComplete ( schName : string ) ( waitTime:int ) =
    let schedulerRows = "//div[contains(@class,'schedule-items-grid')]/div"
    let schedulerDiv = schedulerRows + sprintf "/div/div/div[@title=\"%s\"]" schName
    let historyBtn = schedulerDiv + "/../div[contains(@class,'historyButton')]/div/*"
    let historyTable = "div.taskHistoryList-grid"
    let firstJobStartLbl = historyTable + ">div:nth-child(2)>div.term-label-startEnd>div>div>p:first-of-type"
    let openSchHistory()=       
        scrollToAboveElement historyBtn 150
        click historyBtn
        let histTableHeader =  historyTable + ">div:first-of-type>div:first-of-type"       
        waitForElementPresent histTableHeader
        scrollTo histTableHeader
    let reloadSchHistory()=
        reloadPage()
        openHostScheduler schName
        openSchHistory()
    //read last Job's start time
    openHostScheduler schName
    openSchHistory()
    let lastJobStartTime = (element firstJobStartLbl).Text
    //start host scheduler job
    reloadPage()
    let runStarted = runHostScheduler schName
    if not(runStarted) then 
        false //job could not start
    else
        openSchHistory()
        let newJobStartTime = (element firstJobStartLbl).Text
        if newJobStartTime = lastJobStartTime then
            false //started job not shown in history table
        else
            let successCheck = historyTable + ">div:nth-child(2)>div.term-label-succeeded>div>div.checkMarkIcon>*"
            //check every 2 sec for job to complete
            let mutable retries = waitTime / 2
            while not(existsAndVisible successCheck) && retries > 0 do
                sleep 2
                reloadSchHistory()
                retries <- retries - 1
            existsAndVisible successCheck

/// <summary>Checks a Host Scheduler exists</summary>
/// <param name="name">Name of the Host Scheduler</param>
/// <returns>failedReasons: String: Reasons for failure, if any.</returns>
let checkSchedulerExists name =     
    let mutable failedReasons = ""
    let taskDiv = sprintf "//div[@title=\"%s\"]" name
    if not(exists taskDiv) then
        failedReasons <- failedReasons + sprintf "Host Scheduler %A does not exist. " name
    else
        scrollToAboveElement taskDiv 200
        if not(existsAndVisible taskDiv) then
            failedReasons <- failedReasons + sprintf "Host Scheduler %A is not visible. " name
        else
            let readTaskName = (element taskDiv).Text
            if not(readTaskName.Contains(name)) then
                failedReasons <- failedReasons + sprintf "Name of Host Scheduler %A was not found within read name %A. " name readTaskName
    //printfn "Scheduler %A exists" name //debug
    failedReasons

/// <summary>Checks a Host Scheduler exists. If so, checks its enabled state, frequency, and retry time lapse. </summary>
/// <param name="schedulerDetails"> The details like name, enabled state, frequency and retry time of the Host Scheduler contained in type hostScheduler. </param>
/// <returns>failedReasons: String: Reasons for failure.</returns>
let checkSchedulerSettings ( schedulerDetails : HostScheduler) =      
    ()

