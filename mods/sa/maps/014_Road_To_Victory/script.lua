BotDelay =
{
	easy = 15,
	normal = 6,
	hard = 3
}

WorldLoaded = function()
	Camera.Position = Actor140.CenterPosition
	Ants = Player.GetPlayer("Ants")
	Wasps = Player.GetPlayer("Wasps")
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
		Ants.GrantCondition("enable-ants-ai")
		Wasps.GrantCondition("enable-wasps-ai")
		Spiders.GrantCondition("enable-spiders-ai")
		Actor136.AttackMove(CPos.New(47,30))
		Actor133.AttackMove(CPos.New(25,54))
	end)
end
