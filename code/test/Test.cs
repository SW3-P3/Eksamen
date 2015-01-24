[TestCase("Lars", Result = "Lars")]
[TestCase("Peter", Result = "Peter")]
[TestCase("Soren", Result = "Soren")]
public string EditName_DifferentNames_ShouldChangeName(string name)
    {
        //PreCondition
        Assert.AreNotEqual(name,_user.Name);

        //Act
        _controller.EditName(_user.Username, name);

        return _user.Name;
}
