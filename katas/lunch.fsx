// types 
type Message = 
    | Init of string list
    | Upvote of string
    | Downvote of string
    | DownAndDestroy of string
    | Render 

type Lunch = { Name : string; Votes : int }






// helper functions 
let merge lunch1 lunch2 = { lunch1 with Votes = lunch1.Votes + lunch2.Votes}
let lunch restaurantName votes = { Name = restaurantName; Votes = votes }
let reduce listOfLunches = 
    listOfLunches 
        |> List.groupBy (fun n -> n.Name)
        |> List.map (fun (name, group) -> group |> List.reduce merge)
        |> List.sortByDescending (fun n -> n.Votes)







// actor
let lunchVoteActor = MailboxProcessor<Message>.Start(fun inbox ->
        let rec loop state = async {
            let! msg = inbox.Receive()

            match msg with
            | Init listOfRestaurantNames -> 
                let empty s = lunch s 0
                let emptyLunches = listOfRestaurantNames
                                    |> List.distinct 
                                    |> List.map empty
                
                return! loop emptyLunches

            | Downvote restaurantName | Upvote restaurantName -> 
                let newLunch = match msg with 
                                    | Downvote _ -> lunch restaurantName -2;
                                    | _ -> lunch restaurantName 1

                let results = newLunch::state 
                                |> reduce

                return! loop results

            | DownAndDestroy restaurantName ->
                let direction name = if name = restaurantName then -2 
                                     else 1
                                     
                let newVotes = state 
                                |> List.map (fun n -> {n with Votes = direction n.Name})

                let results = newVotes @ state
                                 |> reduce
                return! loop results
                
            | Render ->
                state 
                    |> List.map (fun i -> sprintf "%s : %i" i.Name i.Votes)
                    |> List.iter System.Console.WriteLine
                // no change to state here
                return! loop state
        }
        loop []
    )



//shorthand
let render() = lunchVoteActor.Post (Render)
let up rest = 
    lunchVoteActor.Post (Upvote rest)
    
let down rest = 
    lunchVoteActor.Post (Downvote rest)

let dd rest = 
    lunchVoteActor.Post (DownAndDestroy rest)

let init restList = 
    lunchVoteActor.Post (Init restList)



