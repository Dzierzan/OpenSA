BotDelay =
{
	easy = 12,
	normal = 6,
	hard = 3
}

WorldLoaded = function()
	Camera.Position = Actor192.CenterPosition
	Spiders = Player.GetPlayer("Spiders")
	Scorpions = Player.GetPlayer("Scorpions")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
		Spiders.GrantCondition("enable-spiders-ai")
	end)
end