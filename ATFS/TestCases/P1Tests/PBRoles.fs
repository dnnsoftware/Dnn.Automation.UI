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

module PBRoles

open DnnCanopyContext
open DnnAddToRole
open DnnRoles

let mutable private roleName = ""
let mutable private groupName = ""

let private rolesTests _ =
    context "Persona Bar: Manage : Roles Tests"

    "PB Roles | Verify UI elements" @@@ fun _ ->
        loginAsAdmin() |> ignore
        openPBRoles()
        if not(existsAndVisible "//div[@class='groups-filter']") || not(existsAndVisible "div#users-header-row")
            || not(existsAndVisible createRoleBtn) || not(existsAndVisible "div.roles-list-container>div>div>div.search-filter>div>input") then
            failwith "  FAIL: Roles: One or more expected UI elements not found."

    "PB Roles | Create a New Role and Role Group" @@@ fun _ ->
        loginAsAdmin() |> ignore
        openPBRoles()
        let randId = getRandomId()
        roleName <- "TestRole" + randId
        groupName <- "TestRoleGroup" + randId
        createNewRole roleName groupName

    "PB Roles | Edit a Role and Role Group" @@@ fun _ ->
        let newRoleName = roleName + "Edited"
        editRole roleName groupName newRoleName
        roleName <- newRoleName
        let newGroupName = groupName + "Edited"
        editRoleGroup groupName newGroupName
        groupName <- newGroupName

    "PB Roles | Delete a Role and Role Group" @@@ fun _ ->
        deleteRole roleName groupName
        deleteRoleGroup groupName

let all _ = 
    rolesTests()
