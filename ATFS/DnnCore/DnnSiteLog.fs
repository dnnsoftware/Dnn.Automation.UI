module DnnSiteLog

open System
open System.IO
open System.Text.RegularExpressions

let private ignoreFileName = "IngoredLogErrors.txt"

// The following array contains fragments of log file lines which are to be ignored. These are saved in the text file loded below
let private ignoredLogFileErrors = 
    try 
        File.ReadAllLines(Path.Combine(exeLocation, ignoreFileName))
    with ex -> 
        printfn "Error loading %s. %s" ignoreFileName ex.Message
        [| "System.Threading.ThreadAbortException"; "System.OperationCanceledException: The operation was canceled"; 
           "System.Web.HttpException (0x80004005): A potentially dangerous Request" |]

(*
Exception log file entries :
  2015-11-16 16:12:00,501 [DNN-PC11][Thread:39][FATAL] DotNetNuke.Web.Common.Internal.DotNetNukeHttpApplication - System.OperationCanceledException: The operation was canceled.
  2015-11-16 16:12:10,782 [DNN-PC11][Thread:11][ERROR] DotNetNuke.Services.Exceptions.Exceptions - System.InvalidOperationException: Timeout expired.  The timeout period elapsed prior to obtaining a connection from the pool.  This may have occurred because all pooled connections were in use and max pool size was reached.
  2015-11-26 10:25:28,305 [DNN-PC11][Thread:42][FATAL] DotNetNuke.Web.Common.Internal.DotNetNukeHttpApplication - System.Web.HttpException (0x80004005): A potentially dangerous Request.Path value was detected from the client (:).
 *)

// works for 2000 to 2099 only
let private logLineRegex = Regex @"^20\d{2}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}(.|,)\d{3}.+\[(ERROR|FATAL)\] .+?$" 

// returns array of non-white listed errors in the site log file
// the file must exist before calling this
let private getLogFileErrorsInternal (fileinfo : FileInfo) = 
    let isErrorInWhitelist (line : string) = ignoredLogFileErrors |> Array.exists (fun s -> line.IndexOf(s) >= 0)
    seq { 
        let thisdate = DateTime.Now.ToString("yyyy-MM-dd")
        use sr = new StreamReader(fileinfo.FullName)
        while not sr.EndOfStream do
            let line = sr.ReadLine()
            if line.StartsWith thisdate && logLineRegex.IsMatch line then 
                if not (isErrorInWhitelist line) then yield line
    }
    |> Seq.toArray

let private logFileName() = 
    if isRemoteSite then
        None
    elif not (String.IsNullOrEmpty config.Site.WebsiteFolder) then
        let dinfo = DirectoryInfo(Path.Combine(config.Site.WebsiteFolder, "Portals\_default\Logs"))
        if dinfo.Exists then 
            let logFilePattern = sprintf "%d.*.log.resources" DateTime.Now.Year
            let filesList = dinfo.GetFiles(logFilePattern)
            if filesList.Length > 0 then Some(filesList |> Array.last)
            else None
        else None
    else None

//checks the log file for errors
let mutable private oldLogfileSize = 0L

let mutable private oldExceptionsount = 
    match logFileName() with
    | None -> 0
    | Some(lastFile) -> 
        oldLogfileSize <- lastFile.Length
        (getLogFileErrorsInternal lastFile).Length

let analyzeLogFile() = 
    match logFileName() with
    | None -> ()
    | Some(lastFile) -> 
        if oldLogfileSize <> lastFile.Length then 
            if oldLogfileSize > lastFile.Length then oldExceptionsount <- 0 // a new log file was created
            oldLogfileSize <- lastFile.Length
            let errors = getLogFileErrorsInternal lastFile
            if errors.Length > 0 && oldExceptionsount <> errors.Length then 
                //UNDONE: copying the log to "config.Site.BackupLogFilesTo"
                let errsToReport = errors |> Array.skip oldExceptionsount
                let errmsg = sprintf "  The following error(s) found in the log file:\n\t%s" (String.Join("\n\t", errsToReport))
                oldExceptionsount <- errors.Length
                failwith errmsg

// returns array of non-white listed errors in the site log file
let getLogFileErrors() = 
    match logFileName() with
    | None -> [||]
    | Some(lastFile) -> getLogFileErrorsInternal lastFile
