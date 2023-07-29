BotDelay =
{
	easy = 12,
	normal = 6,
	hard = 3
	veryhard = 1,
}

WorldLoaded = function()
	Camera.Position = Actor270.CenterPosition
	Spiders = Player.GetPlayer("Spiders")
	Ants = Player.GetPlayer("Ants")
	Beetles = Player.GetPlayer("Beetles")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
		Spiders.GrantCondition("enable-spiders-ai")
		Ants.GrantCondition("enable-ants-ai")
		Beetles.GrantCondition("enable-beetles-ai")
	end)
end
