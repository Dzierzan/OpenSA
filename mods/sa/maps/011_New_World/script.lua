BotDelay =
{
	easy = 15,
	normal = 6,
	hard = 3,
	veryhard = 1
}

WorldLoaded = function()
	Camera.Position = Actor71.CenterPosition
	Wasps = Player.GetPlayer("Wasps")
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
		Wasps.GrantCondition("enable-wasps-ai")
		Spiders.GrantCondition("enable-spiders-ai")
	end)
end
