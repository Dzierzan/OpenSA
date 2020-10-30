Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor178.CenterPosition

	Spiders = Player.GetPlayer("Spiders")
	Ants = Player.GetPlayer("Ants")
	Beetles = Player.GetPlayer("Beetles")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Spiders.GrantCondition("enable-spiders-ai")
		Ants.GrantCondition("enable-ants-ai")
		Beetles.GrantCondition("enable-beeltes-ai")

	end)

end
