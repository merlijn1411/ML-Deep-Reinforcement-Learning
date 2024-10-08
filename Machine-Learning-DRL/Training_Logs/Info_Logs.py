import logging

# Logger configureren
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

class AgentPerformance:
    def __init__(self, name):
        self.name = name
        self.rewards = []
        self.elo = 5000  # Begin ELO

    def update_performance(self, reward):
        self.rewards.append(reward)
        self.elo += reward  # ELO kan op basis van andere logica worden bijgewerkt

    def average_reward(self):
        return sum(self.rewards) / len(self.rewards) if self.rewards else 0

def get_seeker_reward():
    # Simuleer een reward voor de Seeker agent
    return random.uniform(0, 1)  # Vervang dit met je eigen logica

def get_runner_reward():
    # Simuleer een reward voor de Runner agent
    return random.uniform(0, 1)  # Vervang dit met je eigen logica

# Initialiseer prestaties
seeker_performance = AgentPerformance("Seeker")
runner_performance = AgentPerformance("Runner")

total_steps = 10000  # Aantal stappen in de training

# Voorbeeld trainingsloop
for step in range(total_steps):
    # Hier zou je de logica van je training hebben, inclusief het verkrijgen van rewards
    seeker_reward = get_seeker_reward()  # Functie die beloning voor Seeker haalt
    runner_reward = get_runner_reward()  # Functie die beloning voor Runner haalt
    
    # Prestaties bijwerken
    seeker_performance.update_performance(seeker_reward)
    runner_performance.update_performance(runner_reward)
    
    # Informatie loggen
    if step % 1000 == 0:  # Log elke 1000 stappen
        logger.info(f"Step: {step}, Seeker Reward: {seeker_reward:.2f}, ELO: {seeker_performance.elo}, Average Reward: {seeker_performance.average_reward():.2f}")
        logger.info(f"Step: {step}, Runner Reward: {runner_reward:.2f}, ELO: {runner_performance.elo}, Average Reward: {runner_performance.average_reward():.2f}")