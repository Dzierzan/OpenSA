Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor71.CenterPosition

	Beetles = Player.GetPlayer("Beetles")
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Beetles.GrantCondition("enable-beetles-ai")
		Spiders.GrantCondition("enable-spiders-ai")
	end)

end
