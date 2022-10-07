BotDelay =
{
	easy = 12,
	normal = 6,
	hard = 3
}

WorldLoaded = function()
	Camera.Position = Actor7.CenterPosition
	Ants = Player.GetPlayer("Ants")
	Wasps = Player.GetPlayer("Wasps")
	Spiders = Player.GetPlayer("Spiders")
	Scorpions = Player.GetPlayer("Scorpions")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
		Ants.GrantCondition("enable-ants-ai")
		Wasps.GrantCondition("enable-wasps-ai")
		Spiders.GrantCondition("enable-spiders-ai")
		Scorpions.GrantCondition("enable-scorpions-ai")
	end)
end
