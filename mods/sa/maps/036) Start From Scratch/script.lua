Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor175.CenterPosition

	Beetles = Player.GetPlayer("Beetles")
	Wasps = Player.GetPlayer("Wasps")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Beetles.GrantCondition("enable-beetles-ai")
		Wasps.GrantCondition("enable-wasps-ai")
		Actor139.AttackMove(CPos.New(36,15))
		Actor135.AttackMove(CPos.New(36,15))
		Actor133.AttackMove(CPos.New(36,15))
		Actor132.AttackMove(CPos.New(36,15))
		Actor131.AttackMove(CPos.New(36,15))
		Actor138.AttackMove(CPos.New(36,15))
		Actor137.AttackMove(CPos.New(36,15))
		Actor136.AttackMove(CPos.New(36,15))
		Actor140.AttackMove(CPos.New(36,15))

	end)

end
