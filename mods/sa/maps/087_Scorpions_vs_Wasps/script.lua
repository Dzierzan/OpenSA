BotDelay =
{
	easy = 15,
	normal = 6,
	hard = 3,
	veryhard = 1
}

WorldLoaded = function()
	Camera.Position = Actor199.CenterPosition
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
		Spiders.GrantCondition("enable-spiders-ai")
	end)
end
