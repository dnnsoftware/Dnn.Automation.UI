module AddUsersToRoles

open DnnCanopyContext
open DnnAddToRole

let platformRoles _ = 
    context "Testing User Registration: Platform Roles"

    "Host | Create a user and add to Administrators role" @@@ fun _ -> 
        loginAsAdmin() |> ignore

let all _ =
    platformRoles()
