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
