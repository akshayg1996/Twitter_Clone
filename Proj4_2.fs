open System
open System.Collections.Generic
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.ServerErrors
open Suave.Writers
open Newtonsoft.Json
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket

let system = ActorSystem.Create("TwitterEngine")

let setCORSHeaders =
    setHeader  "Access-Control-Allow-Origin" "*"
    >=> setHeader "Access-Control-Allow-Headers" "content-type"

type Register =
    {
        UserName: string
        Password: string
    }

type Login =
    {
        UserName: string
        Password: string
    }

type Logout =
    {
        UserName: string
    }

type Answer = 
    {
        Text: string
        AnswerId: int
    }

type NewAnswer =
    {
        Text: string
    }

type RespMsg =
    {
        Comment: string
        Content: list<string>
        status: int
        error: bool
    }

type Follower = 
    {
        UserName: string
        Following: string
    }

type NewTweet =
    {
        Tweet: string
        UserName: string
    }

let mutable twitterUsers = Map.empty
let mutable activeTwitterUsers = Map.empty
let mutable ownerOfTweet = Map.empty
let mutable twitterUserFollowers = Map.empty
let mutable twitterUserMentions = Map.empty
let mutable twitterUserHashTags = Map.empty
let mutable twitterUserSocketMap = Map.empty

let buildByteResponseToWS (message:string) =
    message
    |> System.Text.Encoding.ASCII.GetBytes
    |> ByteSegment

type LiveUserHandlerMsg =
    | SendTweet of WebSocket * NewTweet
    | SendMention of WebSocket * NewTweet
    | SelfTweet of WebSocket * NewTweet
    | Following of WebSocket * string

let liveUserHandler (mailbox:Actor<_>) = 
    let rec loop() = actor{
        let! msg = mailbox.Receive()
        match msg with
        |SelfTweet(ws,tweet)->  let resp = "You have tweeted '"+tweet.Tweet+"'"
                                let byteResp = buildByteResponseToWS resp
                                let s =socket{
                                                do! ws.send Text byteResp true
                                                }
                                Async.StartAsTask s |> ignore
        |SendTweet(ws,tweet)->
                                let resp = tweet.UserName+" has tweeted '"+tweet.Tweet+"'"
                                let byteResp = buildByteResponseToWS resp
                                let s =socket{
                                                do! ws.send Text byteResp true
                                                }
                                Async.StartAsTask s |> ignore
        |SendMention(ws,tweet)->
                                let resp = tweet.UserName+" mentioned you in tweet '"+tweet.Tweet+"'"
                                let byteResp = buildByteResponseToWS resp
                                let s =socket{
                                                do! ws.send Text byteResp true
                                                }
                                Async.StartAsTask s |> ignore
        |Following(ws,msg)->
                                let resp = msg
                                let byteResp = buildByteResponseToWS resp
                                let s =socket{
                                                do! ws.send Text byteResp true
                                                }
                                Async.StartAsTask s |> ignore
        return! loop()
    }
    loop()
    
let liveUserHandlerRef = spawn system "luref" liveUserHandler

let addAnotherTwitterUser (user: Register) =
    let temp = twitterUsers.TryFind(user.UserName)
    if temp = None then
        twitterUsers <- twitterUsers.Add(user.UserName,user.Password)
        {Comment = "Registration is Successful!";Content=[];status=1;error=false}
    else
        {Comment = "This User Already Exists!";Content=[];status=1;error=true}

let loginTwitterUser (user: Login) = 
    printfn "Login Request Received from %s as %A" user.UserName user
    let temp = twitterUsers.TryFind(user.UserName)
    if temp = None then
        {Comment = "This user doesn't exist. Kindly register first and then login.";Content=[];status=0;error=true}
    else
        if temp.Value.CompareTo(user.Password) = 0 then
            let temp1 = activeTwitterUsers.TryFind(user.UserName)
            if temp1 = None then
                activeTwitterUsers <- activeTwitterUsers.Add(user.UserName,true)
                {Comment = "User Logged In Successfully";Content=[];status=2;error=false}
            else
                {Comment = "This User is Already Logged In";Content=[];status=2;error=true}
        else
            {Comment = "Wrong Password Entered";Content=[];status=1;error=true}

let logoutTwitterUser (user:Logout) = 
    printfn "Logout Request Received from %s as %A" user.UserName user
    let temp = twitterUsers.TryFind(user.UserName)
    if temp = None then
        {Comment = "This User Does not exist. Kindly Register first and then login.";Content=[];status=0;error=true}
    else
        let temp1 = activeTwitterUsers.TryFind(user.UserName)
        if temp1 = None then
            {Comment = "This User Is Not Logged In";Content=[];status=1;error=true}
        else
            activeTwitterUsers <- activeTwitterUsers.Remove(user.UserName)
            {Comment = "This User Is Logged out Successfully";Content=[];status=1;error=false}

let checkIfUserIsLoggedIn username = 
    let temp = activeTwitterUsers.TryFind(username)
    if temp <> None then
        1 // The user is logged In
    else
        let temp1 = twitterUsers.TryFind(username)
        if temp1 = None then
            -1 // The user doesn't exist
        else
            0 // The user is registered but is not logged in

let checkIfUserIsRegistered username =
    let temp = twitterUsers.TryFind(username)
    temp <> None

let dataParser (tweet:NewTweet) =
    let splits = (tweet.Tweet.Split ' ')
    for i in splits do
        if i.StartsWith "#" then
            let temp1 = i.Split '#'
            let temp = twitterUserHashTags.TryFind(temp1.[1])
            if temp = None then
                let lst = List<string>()
                lst.Add(tweet.Tweet)
                twitterUserHashTags <- twitterUserHashTags.Add(temp1.[1],lst)
            else
                temp.Value.Add(tweet.Tweet)
        elif i.StartsWith "@" then
            let temp = i.Split '@'
            if checkIfUserIsRegistered temp.[1] then
                let temp1 = twitterUserMentions.TryFind(temp.[1])
                if temp1 = None then
                    let mutable mp = Map.empty
                    let tlist = new List<string>()
                    tlist.Add(tweet.Tweet)
                    mp <- mp.Add(tweet.UserName,tlist)
                    twitterUserMentions <- twitterUserMentions.Add(temp.[1],mp)
                else
                    let temp2 = temp1.Value.TryFind(tweet.UserName)
                    if temp2 = None then
                        let tlist = new List<string>()
                        tlist.Add(tweet.Tweet)
                        let mutable mp = temp1.Value
                        mp <- mp.Add(tweet.UserName,tlist)
                        twitterUserMentions <- twitterUserMentions.Add(temp.[1],mp)
                    else
                        temp2.Value.Add(tweet.Tweet)
                let temp3 = twitterUserSocketMap.TryFind(temp.[1])
                if temp3<>None then
                    liveUserHandlerRef <! SendMention(temp3.Value,tweet)

let addFollower (follower: Follower) =
    printfn "Received Follower Request from %s as %A" follower.UserName follower
    let status = checkIfUserIsLoggedIn follower.UserName
    if status = 1 then
        if (checkIfUserIsRegistered follower.Following) then
            let temp = twitterUserFollowers.TryFind(follower.Following)
            let temp1 = twitterUserSocketMap.TryFind(follower.UserName)
            if temp = None then
                let lst = new List<string>()
                lst.Add(follower.UserName)
                twitterUserFollowers <- twitterUserFollowers.Add(follower.Following,lst)
                if temp1 <> None then
                    liveUserHandlerRef <! Following(temp1.Value,"You are now following: "+follower.Following)
                {Comment = "Sucessfully Added to the Following list";Content=[];status=2;error=false}
            else
                if temp.Value.Exists( fun x -> x.CompareTo(follower.UserName) = 0 ) then
                    if temp1 <> None then
                        liveUserHandlerRef <! Following(temp1.Value,"You are already following: "+follower.Following)
                    {Comment = "You are already Following"+follower.Following;Content=[];status=2;error=true}
                else
                    temp.Value.Add(follower.UserName)
                    if temp1 <> None then
                        liveUserHandlerRef <! Following(temp1.Value,"You are now following: "+follower.Following)
                    {Comment = "Sucessfully Added to the Following list";Content=[];status=2;error=false}
        else
            {Comment = "Follower "+follower.Following+" doesn't exist";Content=[];status=2;error=true}
    elif status = 0 then
        {Comment = "Please Login";Content=[];status=1;error=true}
    else
        {Comment = "User Doesn't Exist. Kindly Register and then login.";Content=[];status=0;error=true}

let addAnotherTweet (tweet: NewTweet) =
    let temp = ownerOfTweet.TryFind(tweet.UserName)
    if temp = None then
        let lst = new List<string>()
        lst.Add(tweet.Tweet)
        ownerOfTweet <- ownerOfTweet.Add(tweet.UserName,lst)
    else
        temp.Value.Add(tweet.Tweet)

let addTweetToFollowers (tweet: NewTweet) = 
    let temp = twitterUserFollowers.TryFind(tweet.UserName)
    if temp <> None then
        for i in temp.Value do
            let temp1 = {Tweet=tweet.Tweet;UserName=i}
            addAnotherTweet temp1
            let temp2 = twitterUserSocketMap.TryFind(i)
            printfn "%s" i
            if temp2 <> None then
                liveUserHandlerRef <! SendTweet(temp2.Value,tweet)

type tweetHandlerMsg =
    | AddTweetMsg of NewTweet
    | AddTweetToFollowersMsg of NewTweet
    | TweetParserMsg of NewTweet

let dataHandler (mailbox:Actor<_>) =
    let rec loop() = actor{
        let! msg = mailbox.Receive()
        match msg with 
        | AddTweetMsg(tweet) -> addAnotherTweet(tweet)
                                let temp = twitterUserSocketMap.TryFind(tweet.UserName)
                                if temp <> None then
                                    liveUserHandlerRef <! SelfTweet(temp.Value,tweet)
        | AddTweetToFollowersMsg(tweet) ->  addTweetToFollowers(tweet)
        | TweetParserMsg(tweet) -> dataParser(tweet)
        return! loop()
    }
    loop()

let tweetHandlerRef = spawn system "thref" dataHandler

let addTweetToUser (tweet: NewTweet) =
    let status = checkIfUserIsLoggedIn tweet.UserName
    if status = 1 then
        tweetHandlerRef <! AddTweetMsg(tweet)
        tweetHandlerRef <! AddTweetToFollowersMsg(tweet)
        tweetHandlerRef <! TweetParserMsg(tweet)
        {Comment = "Tweeted Successfully";Content=[];status=2;error=false}
    elif status = 0 then
        {Comment = "Please Login";Content=[];status=1;error=true}
    else
        {Comment = "User Doesn't Exsit. Kindly Register first and then login.";Content=[];status=0;error=true}

let getTweets username =
    let status = checkIfUserIsLoggedIn username
    if status = 1 then
        let temp = ownerOfTweet.TryFind(username)
        if temp = None then
            {Comment = "No Tweets";Content=[];status=2;error=false}
        else
            let len = Math.Min(10,temp.Value.Count)
            let res = [for i in 1 .. len do yield(temp.Value.[i-1])] 
            {Comment = "Get Tweets done Successfully";Content=res;status=2;error=false}
    elif status = 0 then
        {Comment = "Please Login";Content=[];status=1;error=true}
    else
        {Comment = "User Doesn't Exist. Kindly Register first and then login.";Content=[];status=0;error=true}

let getMentions username = 
    let status = checkIfUserIsLoggedIn username
    if status = 1 then
        let temp = twitterUserMentions.TryFind(username)
        if temp = None then
            {Comment = "No Mentions";Content=[];status=2;error=false}
        else
            let res = new List<string>()
            for i in temp.Value do
                for j in i.Value do
                    res.Add(j)
            let len = Math.Min(10,res.Count)
            let res1 = [for i in 1 .. len do yield(res.[i-1])] 
            {Comment = "Get Mentions done Succesfully";Content=res1;status=2;error=false}
    elif status = 0 then
        {Comment = "Please Login";Content=[];status=1;error=true}
    else
        {Comment = "User Doesn't Exist. Kindly Register first and then login.";Content=[];status=0;error=true}

let getHashTags username hashtag =
    let status = checkIfUserIsLoggedIn username
    if status = 1 then
        printf "%s" hashtag
        let temp = twitterUserHashTags.TryFind(hashtag)
        if temp = None then
            {Comment = "No Tweets with this hashtag found";Content=[];status=2;error=false}
        else
            let len = Math.Min(10,temp.Value.Count)
            let res = [for i in 1 .. len do yield(temp.Value.[i-1])] 
            {Comment = "Get Hashtags done Succesfully";Content=res;status=2;error=false}
    elif status = 0 then
        {Comment = "Please Login";Content=[];status=1;error=true}
    else
        {Comment = "User Doesn't Exist. Kindly Register first and then login.";Content=[];status=0;error=true}

let registerNewUser (user: Register) =
    printfn "Received Register Request from %s as %A" user.UserName user
    addAnotherTwitterUser user

let respTweet (tweet: NewTweet) =
    printfn "Received Tweet Request from %s as %A" tweet.UserName tweet
    addTweetToUser tweet

let getString (rawForm: byte[]) =
    System.Text.Encoding.UTF8.GetString(rawForm)

let fromJson<'a> json =
    JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a

let gettweets username =
    printfn "Received GetTweets Request from %s " username
    getTweets username
    |> JsonConvert.SerializeObject
    |> OK
    >=> setMimeType "application/json"
    >=> setCORSHeaders

let getmentions username =
    printfn "Received GetMentions Request from %s " username
    getMentions username
    |> JsonConvert.SerializeObject
    |> OK
    >=> setMimeType "application/json"
    >=> setCORSHeaders

let gethashtags username hashtag =
    printfn "Received GetHashTag Request from %s for hashtag %A" username hashtag
    getHashTags username hashtag
    |> JsonConvert.SerializeObject
    |> OK
    >=> setMimeType "application/json"
    >=> setCORSHeaders

let register =
    request (fun r ->
    r.rawForm
    |> getString
    |> fromJson<Register>
    |> registerNewUser
    |> JsonConvert.SerializeObject
    |> OK
    )
    >=> setMimeType "application/json"
    >=> setCORSHeaders

let login =
    request (fun r ->
    r.rawForm
    |> getString
    |> fromJson<Login>
    |> loginTwitterUser
    |> JsonConvert.SerializeObject
    |> OK
    )
    >=> setMimeType "application/json"
    >=> setCORSHeaders

let logout =
    request (fun r ->
    r.rawForm
    |> getString
    |> fromJson<Logout>
    |> logoutTwitterUser
    |> JsonConvert.SerializeObject
    |> OK
    )
    >=> setMimeType "application/json"
    >=> setCORSHeaders

let newTweet = 
    request (fun r ->
    r.rawForm
    |> getString
    |> fromJson<NewTweet>
    |> respTweet
    |> JsonConvert.SerializeObject
    |> OK
    )
    >=> setMimeType "application/json"
    >=> setCORSHeaders

let follow =
    request (fun r ->
    r.rawForm
    |> getString
    |> fromJson<Follower>
    |> addFollower
    |> JsonConvert.SerializeObject
    |> OK
    )
    >=> setMimeType "application/json"
    >=> setCORSHeaders

let websocketHandler (webSocket : WebSocket) (context: HttpContext) =
    socket {
        let mutable loop = true

        while loop do
              let! msg = webSocket.read()

              match msg with
              | (Text, data, true) ->
                let str = UTF8.toString data 
                if str.StartsWith("UserName:") then
                    let uname = str.Split(':').[1]
                    twitterUserSocketMap <- twitterUserSocketMap.Add(uname,webSocket)
                    printfn "connected to %s websocket" uname
                else
                    let resp = sprintf "response to %s" str
                    let byteResp = buildByteResponseToWS resp
                    do! webSocket.send Text byteResp true

              | (Close, _, _) ->
                let emptyResponse = [||] |> ByteSegment
                do! webSocket.send Close emptyResponse true
                loop <- false
              | _ -> ()
    }

let allow_cors : WebPart =
    choose [
        OPTIONS >=>
            fun context ->
                context |> (
                    setCORSHeaders
                    >=> OK "CORS approved" )
    ]

let app =
    choose
        [ 
            path "/websocket" >=> handShake websocketHandler 
            allow_cors
            GET >=> choose
                [ 
                path "/" >=> OK "Hello World" 
                pathScan "/gettweets/%s" (fun username -> (gettweets username))
                pathScan "/getmentions/%s" (fun username -> (getmentions username))
                pathScan "/gethashtags/%s/%s" (fun (username,hashtag) -> (gethashtags username hashtag))
                ]

            POST >=> choose
                [   
                path "/newtweet" >=> newTweet 
                path "/register" >=> register
                path "/login" >=> login
                path "/logout" >=> logout
                path "/follow" >=> follow
              ]

            PUT >=> choose
                [ ]

            DELETE >=> choose
                [ ]
        ]

[<EntryPoint>]
let main argv =
    startWebServer defaultConfig app
    0
