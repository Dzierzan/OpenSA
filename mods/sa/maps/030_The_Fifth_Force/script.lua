Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor157.CenterPosition

	Ants = Player.GetPlayer("Ants")
	Wasps = Player.GetPlayer("Wasps")
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Ants.GrantCondition("enable-ants-ai")
		Wasps.GrantCondition("enable-wasps-ai")
		Spiders.GrantCondition("enable-spiders-ai")
		Actor136.AttackMove(CPos.New(19,20))
		Actor135.AttackMove(CPos.New(19,20))
		Actor134.AttackMove(CPos.New(19,20))
		Actor133.AttackMove(CPos.New(48,15))
		Actor149.AttackMove(CPos.New(48,15))
	end)

end
