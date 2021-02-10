BotDelay =
{
	easy = 15,
	normal = 6,
	hard = 3
}

WorldLoaded = function()
	Camera.Position = Actor188.CenterPosition
	Spiders = Player.GetPlayer("Spiders")
	Wasps = Player.GetPlayer("Wasps")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
		Spiders.GrantCondition("enable-spiders-ai")
		Wasps.GrantCondition("enable-wasps-ai")
	end)
end
