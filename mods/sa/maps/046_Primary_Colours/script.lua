Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor132.CenterPosition

	Ants = Player.GetPlayer("Ants")
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Ants.GrantCondition("enable-ants-ai")
		Spiders.GrantCondition("enable-spiders-ai")
	end)

end
