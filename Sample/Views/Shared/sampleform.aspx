<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<dynamic>" %>
<html>
<head>
    <title>sample form</title>
</head>
<body>
<h1>Hello world from view</h1>
<form action="/action6" method="post">
	<input name="somefield" value="<%= Model.FirstName %> <%= Model.LastName %>"/>
	<input type="submit" value="POST"/>
</form>
<hr/>
<form action="/action6" method="get">
    First name: <input name="firstname" value="<%= Model.FirstName %>"/> <br/>
	Last name: <input name="lastname" value="<%= Model.LastName %>"/> <br/>
	Age (years): <input name="age" value="<%= (int)((DateTime.Now - Model.DateOfBirth).TotalDays/365) %>"/> <br/>
	<input type="submit" value="GET"/>
</form>
</body>
</html>