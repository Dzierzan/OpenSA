Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor168.CenterPosition

	Ants = Player.GetPlayer("Ants")
	Beetles = Player.GetPlayer("Beetles")
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Ants.GrantCondition("enable-ants-ai")
		Beetles.GrantCondition("enable-beetles-ai")
		Spiders.GrantCondition("enable-spiders-ai")

	end)

end
