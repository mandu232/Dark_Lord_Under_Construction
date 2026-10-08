class Rabbit(var name: String, var age: Int, var color: String, var health: Int) {
    var isAlive: Boolean = true
    fun eat(){
        if (isAlive) {
            health += 5
        }
    }
    fun sleep(){
        if (isAlive) {
            age += 1
        }
    }
    fun drink(){
        if (isAlive) {
            health += 1
        }
    }
    fun run(){
        if (isAlive) {
            health -= 5
        }
    }
    fun walk(){
        if (isAlive) {
            health -= 1
        }
    }
    fun speak(){
        if (isAlive) {
            println("i am Rabbit")
        }
    }
    fun die(){
        health = 0
        isAlive = false
    }
    fun birth(){
        health = 50
        isAlive = true
    }
}

fun main(){
    val rabbit = Rabbit("Bunny", 2, "White", 100)
    rabbit.eat()
    rabbit.sleep()
    rabbit.drink()
    rabbit.run()
    rabbit.walk()
    rabbit.speak()
}