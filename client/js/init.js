(function($){
  $(function(){
    var username = ""
    $('.sidenav').sidenav();
    $("#logout_link").hide();
    $("#login").hide();
    $("#register").show();
    $("#user_page").hide();
    $("#login_button").click(function() {
      $("#login").show();
      $("#register").hide();
    });

    $("#register_button").click(function() {
      $("#login").hide();
      $("#register").show();
    });

    $("#register_form").click(function() {
       // console.log("register");
       var url = "http://127.0.0.1:8080/register";
       var uname = $("#register_username").val();
       var pwd = $("#register_password").val();
       var data = JSON.stringify({'UserName':uname,"Password":pwd});
       $.ajax({
           type: "POST",
           url: url,
           data: data,
           dataType: "json",
           success: function(data){
               if(data.error){
                   
               }else{
                   // alert(data.Comment);
                   $("#register_username").val("");
                   $("#register_password").val("");
                   $("#login").show();
                   $("#register").hide();
               }
               alert(data.Comment);
               
           }
       });
    });

    $("#login_form").click(function() {
      var url = "http://127.0.0.1:8080/login";
            var uname = $("#login_username").val();
            var pwd = $("#login_password").val();
            var data = JSON.stringify({'UserName':uname,"Password":pwd});
            $.ajax({
                type: "POST",
                url: url,
                data: data,
                dataType: "json",
                success: function(data){
                    if(data.error){
                        if(data.status == 0){
                            alert(data.Comment);
                        }else{
                            username = uname;
                            // alert(data.Comment);
                            $("#login_username").val("");
                            $("#login_password").val("");
                            $("#login").hide();
                            $("#register").hide();
                            $("#logout_link").show();
                            $("#user_page").show();
                            startWebSocket();
                            //userload();
                        }
                        // alert(data.Comment);  
                    }else{
                        username = uname;
                        $("#login_username").val("");
                        $("#login_password").val("");
                        $("#login").hide();
                        $("#register").hide();
                        $("#logout_link").show();
                        $("#user_page").show();
                        startWebSocket();
                        //userload();
                        // alert(data.Comment); 
                    }
                    
                }
            });
   });

   $("#tweet_button").click(function() {
            var url = "http://127.0.0.1:8080/newtweet";
            var tweett = $("#tweet_input").val();
            var data = JSON.stringify({'Tweet':tweett,'UserName':username});
          $.ajax({
              type: "POST",
              url: url,
              data: data,
              dataType: "json",
              success: function(data){
                if(data.error){
                  alert(data.Comment);
                  if(data.status < 2){
                    $("#login").show();
                    $("#register").hide();
                    $("#user_page").hide();
                    $("#tweet_input").val("");
                  }
                  // alert(data.Comment); 
                 }else{
                   alert(data.Comment); 
                  $("#login").hide();
                  $("#register").hide();
                  $("#user_page").show();
                  $("#tweet_input").val("");
                }
                  
              }
          });
 });

   $("#getTweets").click(function(){
    var url = "http://127.0.0.1:8080/gettweets/"+username;
            $.ajax({
                type: "GET",
                url: url,
                dataType: "json",
                success: function(data){
                    if(data.error){
                        alert(data.Comment);
                        if(data.status < 2){
                          $("#login").show();
                          $("#register").hide();
                          $("#user_page").hide();
                        }
                        // alert(data.Comment); 
                    }else{
                        // alert(data.Comment); 
                        $("#login").hide();
                        $("#register").hide();
                        $("#user_page").show();
                        for (i=0;i<data.Content.length;i++){
                            $("#tweets-box").append("<li class=\"collection-item\">"+data.Content[i]+"</li>");
                        }
                    } 
                }
            });
 });

 $("#follow_button").click(function(){
  var url = "http://127.0.0.1:8080/follow";
  var following = $("#follow").val();
  var data = JSON.stringify({'UserName':username,"Following":following});
  $.ajax({
      type: "POST",
      url: url,
      data: data,
      dataType: "json",
      success: function(data){
          
          if(data.error){
              alert(data.Comment);
              if(data.status < 2){
                $("#login").show();
                $("#register").hide();
                $("#user_page").hide();
                $("#follow").val("");
              }
              // alert(data.Comment);  
          }else{
              // username = uname;
               alert(data.Comment);
              $("#login").hide();
              $("#register").hide();
              $("#user_page").show();
              $("#follow").val("");
          }
          
      }
  });
});

$("#hashtag_button").click(function(){
  var hashtag = $("#hashtag").val();
            var url = "http://127.0.0.1:8080/gethashtags/"+username+"/"+hashtag;
            $.ajax({
                type: "GET",
                url: url,
                dataType: "json",
                success: function(data){
                    
                    if(data.error){
                        alert(data.Comment);
                        if(data.status < 2){
                          $("#login").show();
                          $("#register").hide();
                          $("#user_page").hide();
                          $("#hashtag").val("");
                        }
                        // alert(data.Comment); 
                    }else{
                        // alert(data.Comment); 
                        $("#login").hide();
                        $("#register").hide();
                        $("#user_page").show();
                        $("#hashtag").val("");
                        for (i=0;i<data.Content.length;i++){
                            $("#hashtags-box").append("<li class=\"collection-item\">"+data.Content[i]+"</li>");
                        }
                    }
                    
                    
                }
            });
});

$("#logout").click(function(){
  var url = "http://127.0.0.1:8080/logout";
  var data = JSON.stringify({'UserName':username});
  $.ajax({
      type: "POST",
      url: url,
      data: data,
      dataType: "json",
      success: function(data){
          if(data.error){
              // alert(data.Comment); 
          }else{
              
              // alert(data.Comment); 
          }
          $("#login").show();
          $("#register").hide();
          $("#user_page").hide();
          $("#logout_link").hide();
          username = "";
      }
  });
});

 $("#getMentions").click(function(){
  var url = "http://127.0.0.1:8080/getmentions/"+username;
  $.ajax({
      type: "GET",
      url: url,
      dataType: "json",
      success: function(data){
          if(data.error){
              alert(data.Comment);
              if(data.status < 2){
                $("#login").show();
                $("#register").hide();
                $("#user_page").hide();
              }
              // alert(data.Comment); 
          }else{
              // alert(data.Comment); 
                $("#login").hide();
                $("#register").hide();
                $("#user_page").show();
              for (i=0;i<data.Content.length;i++){
                $("#mentions-box").append("<li class=\"collection-item\">"+data.Content[i]+"</li>");
            }
          }
          
          
      }
  });
});

   var output;
        function startWebSocket(){
            var wsUri = "ws://localhost:8080/websocket";
            websocket = new WebSocket(wsUri);
            output = document.getElementById("live-feed");
            websocket.onopen = function(evt) { onOpen(evt) };
            websocket.onclose = function(evt) { onClose(evt) };
            websocket.onmessage = function(evt) { onMessage(evt) };
            websocket.onerror = function(evt) { onError(evt) };
        }
        function onOpen(evt)
        {
            writeToScreen("CONNECTED To Server");
            // doSend("WebSocket rocks");
            var message = "UserName:"+username;
            doSend(message);
        }

        function onClose(evt)
        {
            writeToScreen("DISCONNECTED to the Server");
        }

        function onMessage(evt)
        {
            writeToScreen('<span style="color: blue;"> ' + evt.data+'</span>');
        }

        function onError(evt)
        {
            writeToScreen('<span style="color: red;">ERROR:</span> ' + evt.data);
        }

        function doSend(message)
        {
            // writeToScreen("SENT: " + message);
            console.log("sending"+message) ;
            websocket.send(message);
        }

        function writeToScreen(message)
        {
            var pre = document.createElement("li");
            pre.className = "collection-item"
            pre.innerHTML = message;
            output.appendChild(pre);
        }

  }); // end of document ready
})(jQuery); // end of jQuery name space
