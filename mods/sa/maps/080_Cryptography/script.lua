BotDelay =
{
	easy = 15,
	normal = 6,
	hard = 3
}

WorldLoaded = function()
	Camera.Position = Actor257.CenterPosition
	Wasps = Player.GetPlayer("Wasps")
	Beetles = Player.GetPlayer("Beetles")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
		Beetles.GrantCondition("enable-beetles-ai")
		Wasps.GrantCondition("enable-wasps-ai")
	end)
end
