Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor52.CenterPosition

	Ants = Player.GetPlayer("Ants")
	Beetles = Player.GetPlayer("Beetles")
	Wasps = Player.GetPlayer("Wasps")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Ants.GrantCondition("enable-ants-ai")
		Beetles.GrantCondition("enable-beetles-ai")
		Wasps.GrantCondition("enable-wasps-ai")
		Actor47.AttackMove(CPos.New(70,38))
		Actor45.AttackMove(CPos.New(9,37))
		Actor46.AttackMove(CPos.New(9,37))
	end)

end
