BotDelay =
{
	easy = 15,
	normal = 6,
	hard = 3,
	veryhard = 1
}

WorldLoaded = function()
	Camera.Position = Actor171.CenterPosition
	Wasps = Player.GetPlayer("Wasps")
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(BotDelay[Map.LobbyOption("difficulty")]), function()
		Wasps.GrantCondition("enable-wasps-ai")
		Spiders.GrantCondition("enable-spiders-ai")
		Actor167.AttackMove(CPos.New(36,82))
		Actor168.AttackMove(CPos.New(36,82))
		Actor157.AttackMove(CPos.New(36,82))
		Actor163.AttackMove(CPos.New(36,82))
		Actor156.AttackMove(CPos.New(22,62))
	end)
end
