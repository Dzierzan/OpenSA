Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor343.CenterPosition

	Wasps = Player.GetPlayer("Wasps")
	Spiders = Player.GetPlayer("Spiders")
	Beetles = Player.GetPlayer("Beetles")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Wasps.GrantCondition("enable-wasps-ai")
		Spiders.GrantCondition("enable-spiders-ai")
		Beetles.GrantCondition("enable-beetles-ai")

	end)

end
