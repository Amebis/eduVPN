function toggle(id)
{
    var el = document.getElementById(id);
    if (el.style.display == "block") {
        el.style.display = "none";
        el.style.visibility = "hidden";
    }
    else
    {
        el.style.display = "block";
        el.style.visibility = "visible";
    }
}
