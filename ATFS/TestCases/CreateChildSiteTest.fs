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

module CreateChildSiteTest

open DnnCanopyContext
open DnnSiteCreate
open DnnAddToRole
open DnnManager

(*
 * NOTE: This file MUST contain a single test to create a child site and that is aall we need
 *       Afterwards, the main program will will run all regitered tests on this child site.
 *)

let private positive _ = 
    context "Create Child Site Tests"
    "Create childsite" @@@ fun _ -> 
        canopy.configuration.skipAllTestsOnFailure <- true
        createAutomationChildSite()
        clearStoredUsers()
        clearStoredFlags()
        clearBacklogSitePages()
        canopy.configuration.skipAllTestsOnFailure <- false
        useChildPortal <- true // from now on all tests are performed on child site

let all _ = 
    positive()
