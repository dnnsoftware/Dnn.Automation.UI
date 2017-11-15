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

module Modules

open DnnCanopyContext
open DnnExtensions
open DnnUserLogin
open DnnPBConfig

/// <summary>Checks that a module exists by running a SQL query</summary>
/// <param name="moduleName">Name of the module</param>
let private checkModuleExists moduleName =          
    let sqlquery = sprintf "SELECT * FROM {databaseOwner}{objectQualifier}DesktopModules Where (FriendlyName like '%s')" moduleName
    executeSqlQuery sqlquery
    let noResult = "div.no-data"
    if existsAndVisible noResult then
       sprintf "\n\tModule %A does not exist" moduleName
    else ""

let private basicTests _ =

    context "Modules: Module usage tests"  

    "Modules | Verify Console and Authentication module usage" @@@ fun _ ->
        logOff()
        loginAsHost()
        verifyModuleUsage "Console"
        verifyModuleUsage "Authentication"

    context "Modules: Modules available tests" 

    "Modules | Verify removal of old Telerik-dependent modules" @@@ fun _ ->
        logOff()
        loginAsHost()
        let sqlQueryOldModules =
            "SELECT * FROM {databaseOwner}{objectQualifier}DesktopModules Where FriendlyName IN " +
            "('Configuration Manager', 'Dashboard', 'Device Preview Management', 'Extensions', " +
            "'Google Analytics Professional', 'Host Settings', 'File Integrity Checker', " +
            "'License Activation Manager', 'Lists', 'Pages', 'Recycle Bin', 'Scheduler', " +
            "'Security Center', 'Site Groups', 'Portals', 'Site Wizard', 'SQL', " +
            "'Taxonomy Manager', 'Themes', 'User Switcher', 'Web Server Manager')"
        executeSqlQuery sqlQueryOldModules 
        let noResult = "//div[@class='no-data']"
        if not(existsAndVisible noResult) then
            failwith "  FAIL: Some old Telerik-dependent modules are still available"

    "Modules | Verify modules available in package" @@@ fun _ ->
        logOff()
        loginAsHost()
        let testList = platformModuleList
        let mutable failedReasons = ""
        failedReasons <- testList |> List.fold (fun acc field -> acc + checkModuleExists field) failedReasons
        if failedReasons <> "" then failwithf "  FAIL:%s" failedReasons

let all _ =
    basicTests()

