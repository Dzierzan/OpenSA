Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor91.CenterPosition


	Ants = Player.GetPlayer("Ants")
	Beetles = Player.GetPlayer("Beetles")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Ants.GrantCondition("enable-ants-ai")
		Beetles.GrantCondition("enable-beetles-ai")
		Actor38.Attack(Actor62)
		Actor43.Attack(Actor62)
	end)

end
