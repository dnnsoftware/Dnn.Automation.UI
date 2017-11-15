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

module SupportIssues

open canopy
open DnnCanopyContext
open DnnAddToRole
open DnnCreatePage
open DnnAddModuleToPage
open DnnHost

let mutable testPassed = false
let mutable modulesOnPage = 0
let mutable hostModuleOnPage = false
let mutable deployedModuleId = 0
let mutable deployedPageUrl = ""
let mutable pageUrl = ""
let mutable modID =""
let mutable resourcefolder =""

let verifyModuleForUser hostmodule modulename displayname deployedPageUrl=
        let modId, pgUrl = deployModuleOnPage hostmodule modulename displayname deployedPageUrl 
        deployedModuleId <- modId

let SupportIssue _ =
    context "CONTENT-6159 SI: Module hidden to Admin Users (Amrit)"

    "Preconditions for Module hidden to Admin Users " @@@ fun _ ->
        loginAsHost() |> ignore
        let newPage = getNewPageInfo "TestPage"      
        createPage newPage |> ignore
        pageUrl <- currentUrl()
        verifyModuleForUser false "DNNCorpGoogleAnalytics" "Google Analytics Professional" pageUrl
        changeModulePermission pageUrl 1 3 8 2

    "Verify the Module is visible to Admin " @@@ fun _ ->    
        loginAsAdmin() |> ignore
        goto pageUrl
        let moduleonPage = sprintf"//a[@name='%i']/.." deployedModuleId
        if not (existsAndVisible moduleonPage) then
            failwith "  FAIL: Module is not visible to Admin"

let all _ =
    SupportIssue()
