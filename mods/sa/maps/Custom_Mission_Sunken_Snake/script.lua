BotDelay =
{
	easy = 9,
	normal = 3,
	hard = 1
}

WorldLoaded = function()
	Camera.Position = Actor341.CenterPosition
	Ants = Player.GetPlayer("Ants")
	Wasps = Player.GetPlayer("Wasps")
	Beetles = Player.GetPlayer("Beetles")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
		Ants.GrantCondition("enable-ants-ai")
		Wasps.GrantCondition("enable-wasps-ai")
		Beetles.GrantCondition("enable-beetles-ai")
	end)
end
