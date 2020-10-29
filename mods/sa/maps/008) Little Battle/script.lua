Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor154.CenterPosition

	Wasps = Player.GetPlayer("Wasps")
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Wasps.GrantCondition("enable-wasps-ai")
		Spiders.GrantCondition("enable-spiders-ai")
		Actor137.AttackMove(CPos.New(25,29))
		Actor135.AttackMove(CPos.New(25,29))
		Actor136.AttackMove(CPos.New(25,29))
		Actor134.AttackMove(CPos.New(25,29))
		Actor55.AttackMove(CPos.New(105,25))
		Actor54.AttackMove(CPos.New(105,25))

	end)

end
