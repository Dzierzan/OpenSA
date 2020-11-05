Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor206.CenterPosition

	Beetles = Player.GetPlayer("Beetles")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Beetles.GrantCondition("enable-beetles-ai")

	end)

end
