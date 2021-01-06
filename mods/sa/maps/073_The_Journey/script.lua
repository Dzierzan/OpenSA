Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor165.CenterPosition

	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Spiders.GrantCondition("enable-spiders-ai")

	end)

end
