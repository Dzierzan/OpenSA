Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor59.CenterPosition

	Ants = Player.GetPlayer("Ants")
	Beetles = Player.GetPlayer("Beetles")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Ants.GrantCondition("enable-ants-ai")
		Beetles.GrantCondition("enable-beetles-ai")
		Actor53.AttackMove(CPos.New(38,77))
	end)

end
