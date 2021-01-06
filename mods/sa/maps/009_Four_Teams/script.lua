Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor157.CenterPosition


	Ants = Player.GetPlayer("Ants")
	Beetles = Player.GetPlayer("Beetles")
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Ants.GrantCondition("enable-ants-ai")
		Beetles.GrantCondition("enable-beetles-ai")
		Spiders.GrantCondition("enable-spiders-ai")
		Actor87.AttackMove(CPos.New(86,35))
		Actor41.AttackMove(CPos.New(47,85))
	end)

end
