BotDelay =
{
	easy = 15,
	normal = 6,
	hard = 3,
	veryhard = 1
}

WorldLoaded = function()
	Camera.Position = Actor68.CenterPosition
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
		Spiders.GrantCondition("enable-spiders-ai")
		Actor61.Attack(Actor40)
		Actor59.Attack(Actor40)
		Actor66.Attack(Actor40)
		Actor67.Attack(Actor40)
	end)
end
