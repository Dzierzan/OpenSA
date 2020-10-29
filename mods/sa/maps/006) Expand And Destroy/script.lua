Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor68.CenterPosition

	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Spiders.GrantCondition("enable-spiders-ai")
		Actor61.Attack(Actor40)
		Actor59.Attack(Actor40)
		Actor66.Attack(Actor40)
		Actor67.Attack(Actor40)
	end)

end
