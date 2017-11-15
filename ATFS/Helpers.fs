// Automation UI Testing Framework - ATFS - http://www.dnnsoftware.com
// Copyright (c) 2015 - 2017, DNN Corporation
// All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.

[<AutoOpen>]
module Helpers

open System
open System.Diagnostics
open System.IO
open System.Reflection
open System.Threading
open HttpFs.Client

/// <summary>
/// Location of running executable file
/// </summary>
let exeLocation =
    let p = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
    DirectoryInfo(p).FullName

/// <summary>
/// Location of running executable file
/// </summary>
let additionalFilesLocation = DirectoryInfo(Path.Combine(exeLocation, @"..\zipfiles\AdditionalFiles")).FullName

/// <summary>Generates and returns a random integer between two specified numbers.</summary>
/// <param name="min">Minimum integer the generated number will be (inclusive).</param>
/// <param name="max">Maximum integer the generated number will be (exclusive).</param>
let getRandomNumber min max = Random().Next(min, max)
let getRandomId() = (getRandomNumber 1000 10000).ToString()

/// <summary>Generates and returns a random (all lowercase) string of a specified length.</summary>
/// <param name="len">Length of the generated string</param>
/// <param name="max">Maximum integer the generated number will be (exclusive).</param>
let getRandomStr len = 
    if len < 0 then
        raise ( ArgumentOutOfRangeException("len") )
    if len = 0 then
        String.Empty
    else
        let rand = Random()
        let chars = [|'a'..'z'|]
        let f = (fun _ -> chars.[rand.Next(chars.Length)])
        String( Array.init len f )

/// <summary>Cleans a given string from invalid file name characters.</summary>
/// <param name="name">String to clean invalid characters from.</param>
/// <returns>The same string with invalid characters removed.</returns>
let sanitizeFileName name = 
    let chars = Path.GetInvalidFileNameChars()
    let rec clean (s : string) =
        let i = s.IndexOfAny(chars)
        if i < 0 then s
        else clean (s.Remove(i, 1))
    if isNull name then
        name
    else clean name

/// <summary>Executes an external application as another process in the system</summary>
/// <param name="application">Fully qualified path to the application.</param>
/// <param name="arguments">Arguments to pass to the application.</param>
/// <returns>Two sequences of text lines written to the console by the new process;
/// one for the normal lines and another for the error lines.</returns>
let execApplication application arguments =
    printfn "  Executing => %s %s" application arguments
    let procStartInfo = 
        ProcessStartInfo(
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            FileName = application,
            Arguments = arguments
        )

    let outputs = System.Collections.Generic.List<string>()
    let errors = System.Collections.Generic.List<string>()
    let outputHandler f (_sender:obj) (args:DataReceivedEventArgs) =
        printfn "  () ==> %s" args.Data
        f args.Data
    use p = new Process(StartInfo = procStartInfo)
    p.OutputDataReceived.AddHandler(DataReceivedEventHandler (outputHandler outputs.Add))
    p.ErrorDataReceived.AddHandler(DataReceivedEventHandler (outputHandler errors.Add))
    let started = 
        try
            p.Start()
        with | ex ->
            ex.Data.Add("filename", application)
            reraise()
    if not started then
        failwithf "Failed to start process: %s" application
    printfn "Started %s with pid %i" p.ProcessName p.Id
    p.BeginOutputReadLine()
    p.BeginErrorReadLine()
    p.WaitForExit()
    let cleanOut l = l |> Seq.filter (String.IsNullOrEmpty >> not)
    cleanOut outputs, cleanOut errors

/// <summary>Executes a shell command</summary>
/// <param name="command">The command to execute by the shell.</param>
/// <returns>Two sequences of text lines written to the console by the new process;
/// one for the normal lines and another for the error lines.</returns>
let execCommandShell command =
    execApplication "cmd" ("/c " + command)

/// <summary>Retrieve a full web page for a given URL.</summary>
/// <param name="url">Fully qualified URL.</param>
/// <param name="tout">Request timeout in seconds.</param>
/// <returns>The HTML page content.</returns>
let readWebPage url toutSeconds =
    let toutms = (if toutSeconds <= 0 || toutSeconds > 180 then 180 else toutSeconds) * 1000<ms>
    Request.createUrl Get url
    |> Request.timeout toutms
    |> Request.responseAsString
    |> Hopac.Hopac.run

/// <summary>Retries an actions few times while the passed function returns false.</summary>
/// <param name="retryCount">The number of times to retry before giving up</param>
/// <param name="retryDelay">Number of seconds to wait between retries</param>
/// <param name="f">A function that returns a boolean result (true/false)</param>
/// <returns>True if the function returned true in any iteration, false if it returned false in all iterations.</returns>
/// <remarks>This function will exit as soon as the function returns true and will not go over all iterations.</remarks>
let retryWithWait retryCount retryDelay f =
    let rec retry times =
        if times <= 0 then false
        else if f() then true
        else
            match box retryDelay with
            | :? int as i -> Thread.Sleep i
            | :? TimeSpan as t -> Thread.Sleep t
            | _ -> () // ignore delay
            retry (times - 1)
    retry retryCount

/// <summary>Retries an actions few times while the passed function returns false.</summary>
/// <param name="retryCount">The number of times to retry before giving up</param>
/// <param name="f">A function that returns a boolean result (true/false)</param>
/// <returns>True if the function returned true in any iteration, false if it returned false in all iterations</returns>
/// <remarks>This function will exit as soon as the function returns true and will not go over all iterations.</remarks>
let retryWithNoWait retryCount f = retryWithWait retryCount 0 f

/// <summary>Finds a file inside a folder</summary>
/// <param name="folderPath">The path of the folder</param>
/// <param name="searchString">The string that will be used to search for the file, e.g. *myModule*Install.zip will find DNN_myModule_version8_Install.zip</param>
/// <returns>Path of the first matching file</returns>
let findFileInFolder folderPath searchString = 
    let files = Directory.GetFiles(folderPath, searchString)
    if files.Length = 0 then None
    else Some (files.[0])

let fixfilePath path = FileInfo(path).FullName