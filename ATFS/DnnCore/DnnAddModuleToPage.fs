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

module DnnAddModuleToPage

open canopy
open System
open DnnAddToRole
open DnnHost
open DnnManager

let addModuleToPlatformPage pageUrl moduleName displayName = 
    goto pageUrl
    addModuleToPagePB displayName

    //get the latest added module id
    let mutable idx = 0
    let modules = sprintf "//div[contains(@class, 'DnnModule-%s')]/a" moduleName |> elements
    for m in modules do
        let mid = int (m.GetAttribute("name"))
        if mid > idx then
            idx <- mid    
    idx

let isAddModuleSuceccessful () =
    not (existsAndVisible SkinMsgErrorSelector)

/// <summary>Deploys a module to a new or existing page</summary>
/// <param name="hostmodule">Some modules are deployable only by Host. True if this is the case</param>
/// <param name="modulename">The name of the module in code</param>
/// <param name="displayname">The display name of the module in UI</param>
/// <param name="pageUrl">The Url of the page where the module should be deployed. If empty, a new page will be created.</param>
/// <returns>The Module ID of deployed module</returns>
/// <returns>The Url of the page where the module was deployed</returns>
let deployModuleOnPage hostmodule modulename displayname (pageUrl:string) =
    if hostmodule then loginAsHost()
    else loginAsAdmin() |> ignore
    let mutable deployPageUrl = pageUrl
    if String.IsNullOrEmpty(pageUrl) then  
        //let newPage = getNewPageInfo modulename      
        deployPageUrl <- openNewPage true
    let mutable moduleID = 0
    moduleID <- addModuleToPlatformPage deployPageUrl modulename displayname
    if checkForErrorMessageExists() then
        failwithf "  FAIL: Module %A shows error when deployed on a new page (in Edit Mode)" displayname
    if checkForErrorMessageExists() then
        failwithf "  FAIL: Module %A shows error when deployed on a new page (in Published Mode)" displayname
    moduleID, deployPageUrl

/// <summary>Opens module settings and checks for error</summary>
/// <param name="moduleDisplayName">The name of the module in code</param>
/// <param name="handleNumber">Position of the given module's handle on the page (starting at 0, from the top)</param>
let checkModuleSettings moduleDisplayName (handleNumber:int) =
    openModuleSettings "" handleNumber |> ignore    
    if not(existsAndVisible moduleSettingsDiv) || checkForErrorMessageExists() then
        closePopup()
        failwithf "  FAIL: Module %A has an error in Module Settings" moduleDisplayName
    closePopup()
