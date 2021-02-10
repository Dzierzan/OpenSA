BotDelay =
{
	easy = 15,
	normal = 6,
	hard = 3
}

WorldLoaded = function()
	Camera.Position = Actor83.CenterPosition
	Ants = Player.GetPlayer("Ants")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
		Ants.GrantCondition("enable-ants-ai")
	end)
end
